using Newtonsoft.Json;
using RolgutXmlFromApi.Data;
using RolgutXmlFromApi.DTOs;
using RolgutXmlFromApi.Helpers;

using RolgutXmlFromApi.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace RolgutXmlFromApi.Services
{
    public class GaskaApiService
    {
        private readonly GaskaApiSettings _apiSettings;
        private readonly int _minProductPrice;

        public GaskaApiService(GaskaApiSettings apiCredentials)
        {
            _apiSettings = apiCredentials;
            _minProductPrice = AppSettingsLoader.GetMinProductPriceToFetch();
        }

        public async Task SyncProducts()
        {
            HashSet<int> fetchedProductIds = new HashSet<int>();
            bool hasErrors = false;

            using (var client = new HttpClient())
            {
                ApiHelper.AddDefaultHeaders(_apiSettings, client);

                foreach (var categoryId in _apiSettings.CategoriesId)
                {
                    int page = 1;
                    bool hasMore = true;

                    while (hasMore)
                    {
                        try
                        {
                            var url = $"{_apiSettings.BaseUrl}/products?category={categoryId}&page={page}&perPage={_apiSettings.ProductsPerPage}&lng=pl";
                            Log.Information($"Sending request to {url}.");
                            var response = await client.GetAsync(url);

                            if (!response.IsSuccessStatusCode)
                            {
                                Log.Error($"API error while fetching page {page} for category {categoryId}: {response.StatusCode}");
                                hasErrors = true;
                                break;
                            }

                            var json = await response.Content.ReadAsStringAsync();
                            var apiResponse = JsonConvert.DeserializeObject<ApiProductsResponse>(json);

                            if (apiResponse.Products == null || apiResponse.Products.Count == 0)
                            {
                                hasMore = false;
                                break;
                            }

                            using (var db = new MyDbContext())
                            {
                                using (var transaction = db.Database.BeginTransaction())
                                {
                                    foreach (var apiProduct in apiResponse.Products)
                                    {
                                        fetchedProductIds.Add(apiProduct.Id);
                                        var product = db.Products.FirstOrDefault(p => p.Id == apiProduct.Id);

                                        if (product == null)
                                        {
                                            product = new Product { Id = apiProduct.Id };
                                            db.Products.Add(product);
                                        }

                                        // Update fields
                                        product.CodeGaska = apiProduct.CodeGaska;
                                        product.CodeCustomer = apiProduct.CodeCustomer;
                                        product.Name = apiProduct.Name;
                                        product.Description = apiProduct.Description;
                                        product.Ean = apiProduct.Ean;
                                        product.TechnicalDetails = apiProduct.TechnicalDetails;
                                        product.WeightGross = apiProduct.GrossWeight;
                                        product.WeightNet = apiProduct.NetWeight;
                                        product.SupplierName = apiProduct.SupplierName;
                                        product.SupplierLogo = apiProduct.SupplierLogo;
                                        product.InStock = apiProduct.InStock;
                                        product.CurrencyPrice = apiProduct.CurrencyPrice;
                                        product.PriceNet = apiProduct.NetPrice;
                                        product.PriceGross = apiProduct.GrossPrice;
                                    }

                                    await db.Database.ExecuteSqlCommandAsync("SET IDENTITY_INSERT dbo.Products ON;");
                                    await db.SaveChangesAsync();
                                    await db.Database.ExecuteSqlCommandAsync("SET IDENTITY_INSERT dbo.Products OFF;");

                                    transaction.Commit();
                                    Log.Information($"Successfully fetched and updated {apiResponse.Products.Count} products for category {categoryId}.");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Error while getting products from page {page} for category {categoryId}.");
                            hasErrors = true;
                            break;
                        }
                        finally
                        {
                            page++;
                            await Task.Delay(TimeSpan.FromSeconds(_apiSettings.ProductsInterval));
                        }
                    }
                }
            }

            if (hasErrors)
            {
                Log.Warning("Errors occurred during product sync. Archiving skipped to avoid data inconsistency.");
                return;
            }

            try
            {
                Log.Information("Searching for products to archive.");
                int archivedCount = 0;

                using (var db = new MyDbContext())
                {
                    db.Database.CommandTimeout = 240;

                    await db.Products
                        .Where(p => !fetchedProductIds.Contains(p.Id) && !p.Archived)
                        .ForEachAsync(p =>
                        {
                            p.Archived = true;
                            archivedCount++;
                        });

                    await db.SaveChangesAsync();
                    Log.Information($"Archived {archivedCount} products.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while checking for products to archive.");
            }
        }

        public async Task SyncProductDetails()
        {
            List<Product> productsToUpdate = new List<Product>();

            using (var db = new MyDbContext())
            {
                try
                {
                    // Get products that are missing the details collections
                    productsToUpdate = await db.Products
                        .Where(p => !p.Categories.Any() && p.PriceNet >= _minProductPrice && !p.Archived)
                        .Take(_apiSettings.ProductPerDay)
                        .ToListAsync();

                    // If nothing was found, fallback to products ordered by UpdatedDate
                    if (!productsToUpdate.Any())
                    {
                        productsToUpdate = await db.Products
                            .Where(p => p.PriceNet >= _minProductPrice && !p.Archived)
                            .OrderBy(p => p.UpdatedDate)
                            .Take(_apiSettings.ProductPerDay)
                            .ToListAsync();
                    }

                    if (!productsToUpdate.Any())
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error while getting products to update details from database");
                    return;
                }

                using (var client = new HttpClient())
                {
                    ApiHelper.AddDefaultHeaders(_apiSettings, client);

                    foreach (var product in productsToUpdate)
                    {
                        try
                        {
                            var response = await client.GetAsync($"{_apiSettings.BaseUrl}/product?id={product.Id}&lng=pl");

                            if (!response.IsSuccessStatusCode)
                            {
                                Log.Error($"API error while fetching product details. Product ID: {product.Id}. Product Code: {product.CodeGaska}. Response Status: {response.StatusCode}");
                                continue;
                            }

                            var json = await response.Content.ReadAsStringAsync();
                            var apiResponse = JsonConvert.DeserializeObject<ApiProductResponse>(json);

                            if (apiResponse?.Product == null)
                            {
                                continue;
                            }

                            var updatedProduct = apiResponse.Product;

                            // Clear existing collections
                            db.Packages.RemoveRange(product.Packages);
                            db.CrossNumbers.RemoveRange(product.CrossNumbers);
                            db.Components.RemoveRange(product.Components);
                            db.RecommendedParts.RemoveRange(product.RecommendedParts);
                            db.Applications.RemoveRange(product.Applications);
                            db.ProductParameters.RemoveRange(product.Parameters);
                            db.ProductImages.RemoveRange(product.Images);
                            db.ProductFiles.RemoveRange(product.Files);
                            db.ProductCategories.RemoveRange(product.Categories);

                            // Update fields
                            product.CodeGaska = updatedProduct.CodeGaska;
                            product.CodeCustomer = updatedProduct.CodeCustomer;
                            product.Name = updatedProduct.Name;
                            product.SupplierName = updatedProduct.SupplierName;
                            product.SupplierLogo = updatedProduct.SupplierLogo;
                            product.InStock = updatedProduct.InStock;
                            product.CurrencyPrice = updatedProduct.CurrencyPrice;
                            product.PriceNet = updatedProduct.PriceNet;
                            product.PriceGross = updatedProduct.PriceGross;

                            // Map updated collections
                            product.Packages = updatedProduct.Packages.Select(p => new Package
                            {
                                PackUnit = p.PackUnit,
                                PackQty = p.PackQty,
                                PackNettWeight = p.PackNettWeight,
                                PackGrossWeight = p.PackGrossWeight,
                                PackEan = p.PackEan,
                                PackRequired = p.PackRequired
                            }).ToList();

                            product.CrossNumbers = (updatedProduct.CrossNumbers ?? Enumerable.Empty<ApiCrossNumber>())
                                .Where(c => c != null && !string.IsNullOrEmpty(c.CrossNumber))
                                .SelectMany(c => c.CrossNumber
                                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(cn => new CrossNumber
                                    {
                                        CrossNumberValue = cn.Trim(),
                                        CrossManufacturer = c.CrossManufacturer
                                    }))
                                .ToList();

                            product.Components = updatedProduct.Components.Select(c => new Component
                            {
                                TwrID = c.TwrID,
                                CodeGaska = c.CodeGaska,
                                Qty = c.Qty
                            }).ToList();

                            product.RecommendedParts = updatedProduct.RecommendedParts.Select(r => new RecommendedPart
                            {
                                TwrID = r.TwrID,
                                CodeGaska = r.CodeGaska,
                                Qty = r.Qty
                            }).ToList();

                            product.Applications = updatedProduct.Applications.Select(a => new Application
                            {
                                ApplicationId = a.Id,
                                ParentID = a.ParentID,
                                Name = a.Name
                            }).ToList();

                            product.Parameters = updatedProduct.Parameters.Select(p => new ProductParameter
                            {
                                AttributeId = p.AttributeId,
                                AttributeName = p.AttributeName,
                                AttributeValue = p.AttributeValue
                            }).ToList();

                            product.Images = updatedProduct.Images.Select(i => new ProductImage
                            {
                                Title = i.Title,
                                Url = i.Url
                            }).ToList();

                            product.Files = updatedProduct.Files.Select(f => new ProductFile
                            {
                                Title = f.Title,
                                Url = f.Url
                            }).ToList();

                            product.Categories = updatedProduct.Categories.Select(c => new ProductCategory
                            {
                                CategoryId = c.Id,
                                ParentID = c.ParentID,
                                Name = c.Name
                            }).ToList();

                            await db.SaveChangesAsync();
                            Log.Information($"Successfully fetched and updated details of product with ID: {product.Id} and Code: {product.CodeGaska}");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, $"Error while trying to fetch and update product data. Product with ID: {product.Id} and code {product.CodeGaska}");
                            continue;
                        }
                        finally
                        {
                            Task.Delay(TimeSpan.FromSeconds(_apiSettings.ProductInterval)).Wait(); // Respect API rate limits
                        }
                    }
                }
            }
        }
    }
}