using APIDataSyncXMLGenerator.Models;
using Newtonsoft.Json;
using RolgutXmlFromApi.Data;
using RolgutXmlFromApi.DTOs;
using RolgutXmlFromApi.Helpers;

using RolgutXmlFromApi.Helpers;

using RolgutXmlFromApi.Models;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace RolgutXmlFromApi.Services
{
    public class GaskaApiService
    {
        private readonly GaskaApiSettings _apiSettings;

        public GaskaApiService(GaskaApiSettings apiCredentials)
        {
            _apiSettings = apiCredentials;
        }

        public async Task SyncProducts()
        {
            int page = 1;
            bool hasMore = true;

            using (var client = new HttpClient())
            {
                ApiHelper.AddDefaultHeaders(_apiSettings, client);
                while (hasMore)
                {
                    try
                    {
                        var url = $"{_apiSettings.BaseUrl}/products?category={_apiSettings.CategoryId}&page={page}&perPage={_apiSettings.ProductsPerPage}&lng=pl";
                        Log.Information($"Sending request to {url}.");
                        var response = await client.GetAsync(url);

                        if (!response.IsSuccessStatusCode)
                        {
                            Log.Error($"API error while fetching page {page}: {response.StatusCode}");
                            continue;
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
                                    var product = db.Products.FirstOrDefault(p => p.Id == apiProduct.Id);

                                    if (product == null)
                                    {
                                        product = new Product { Id = apiProduct.Id };
                                        db.Products.Add(product);
                                    }

                                    // Basic properties
                                    product.CodeGaska = apiProduct.CodeGaska;
                                    product.CodeCustomer = apiProduct.CodeCustomer;
                                    product.Name = apiProduct.Name;
                                    product.SupplierName = apiProduct.SupplierName;
                                    product.SupplierLogo = apiProduct.SupplierLogo;
                                    product.InStock = apiProduct.InStock;
                                    product.CurrencyPrice = apiProduct.CurrencyPrice;
                                    product.PriceNet = apiProduct.NetPrice;
                                    product.PriceGross = apiProduct.GrossPrice;
                                }

                                // Enable identity insert for the session
                                await db.Database.ExecuteSqlCommandAsync("SET IDENTITY_INSERT dbo.Products ON;");
                                await db.SaveChangesAsync();
                                await db.Database.ExecuteSqlCommandAsync("SET IDENTITY_INSERT dbo.Products OFF;");

                                transaction.Commit();

                                Log.Information($"Successfully fetched and updated {apiResponse.Products.Count} products.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Error while getting products from page {page}.");
                        continue;
                    }
                    finally
                    {
                        page++;
                        Task.Delay(TimeSpan.FromSeconds(_apiSettings.ProductsInterval)).Wait(); // Respect API rate limits
                    }
                }
            }
        }

        public async Task SyncProductDetails()
        {
            List<Product> productsToUpdate = new List<Product>();

            using (var db = new MyDbContext())
            {
                try
                {
                    // Get products that are missing the detail collections
                    productsToUpdate = db.Products
                        .Where(p => !p.Categories.Any())
                        .Take(_apiSettings.ProductPerDay)
                        .ToList();

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

                            product.CrossNumbers = updatedProduct.CrossNumbers
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
                                Id = a.Id,
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