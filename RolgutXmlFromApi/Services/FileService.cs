using RolgutXmlFromApi.Data;
using RolgutXmlFromApi.DTOs;
using RolgutXmlFromApi.Helpers;
using RolgutXmlFromApi.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RolgutXmlFromApi.Services
{
    public class FileService
    {
        private readonly FtpSettings _ftpSettings;

        public FileService(FtpSettings ftpSettings)
        {
            _ftpSettings = ftpSettings;
        }

        public async Task<string> GenerateXMLFile()
        {
            string resultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "result");
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string resultFileName = $"products_{timestamp}.xml";
            string resultFilePath = Path.Combine(resultPath, resultFileName);
            List<Product> products = new List<Product>();

            // Clean up old files
            CleanupOldXmlFiles();

            try
            {
                if (!Directory.Exists(resultPath))
                {
                    Directory.CreateDirectory(resultPath);
                }

                using (var db = new MyDbContext())
                {
                    products = await db.Products
                        .Include(p => p.Categories)
                        .Include(p => p.Applications)
                        .Include(p => p.Parameters)
                        .Include(p => p.CrossNumbers)
                        .Include(p => p.Packages)
                        .Include(p => p.RecommendedParts)
                        .Include(p => p.Components)
                        .Include(p => p.Files)
                        .Include(p => p.Images)
                        .Where(p => p.Categories.Any() && p.Archived == false)
                        .ToListAsync();
                }

                if (products.Any())
                {
                    XmlWriterSettings settings = new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 };

                    using (XmlWriter writer = XmlWriter.Create(resultFilePath, settings))
                    {
                        writer.WriteStartElement("Products");
                        foreach (var product in products)
                        {
                            writer.WriteStartElement("Product");

                            WriteRawElement(writer, "Id", product.Id.ToString());
                            WriteRawElement(writer, "CodeGaska", product.CodeGaska);
                            WriteRawElement(writer, "Name", product.Name);
                            if (!string.IsNullOrEmpty(product.SupplierName))
                                WriteRawElement(writer, "Supplier", product.SupplierName);

                            WriteRawElement(writer, "EAN", product.Ean);
                            WriteRawElement(writer, "WeightNet", product.WeightNet.ToString());
                            WriteRawElement(writer, "WeightGross", product.WeightGross.ToString());
                            WriteRawElement(writer, "Stock", product.InStock.ToString());
                            WriteRawElement(writer, "PriceNet", product.PriceNet.ToString());
                            WriteRawElement(writer, "PriceGross", product.PriceGross.ToString());

                            // Prepare the HTML description
                            var descriptionBuilder = new StringBuilder();
                            if (!string.IsNullOrEmpty(product.Description))
                            {
                                descriptionBuilder.Append("<p><b>Opis: </b>");
                                descriptionBuilder.Append(product.Description);
                                descriptionBuilder.Append("</p>");
                            }

                            if (!string.IsNullOrEmpty(product.Description))
                            {
                                descriptionBuilder.Append("<p><b>Opis techniczny: </b>");
                                descriptionBuilder.Append(product.TechnicalDetails);
                                descriptionBuilder.Append("</p>");
                            }

                            if (product.Parameters != null && product.Parameters.Any())
                            {
                                descriptionBuilder.Append("<p><b>Parametry: </b>");
                                descriptionBuilder.Append(string.Join(", ", product.Parameters?.Select(param => $"{param.AttributeName} : {param.AttributeValue}") ?? new List<string>()));
                                descriptionBuilder.Append("</p>");
                            }

                            if (product.CrossNumbers != null && product.CrossNumbers.Any())
                            {
                                descriptionBuilder.Append("<p><b>Numery referencyjne: </b>");
                                descriptionBuilder.Append(string.Join(", ", product.CrossNumbers.Select(c => c.CrossNumberValue)));
                                descriptionBuilder.Append("</p>");
                            }

                            if (product.Applications != null && product.Applications.Any())
                            {
                                var allApplications = product.Applications
                                    .GroupBy(a => a.ApplicationId)
                                    .ToDictionary(g => g.Key, g => g.First());
                                var parentIds = new HashSet<int>(product.Applications
                                                                 .Where(a => a.ParentID != 0)
                                                                 .Select(a => a.ParentID));

                                var writtenPaths = new HashSet<string>();
                                var applicationPaths = new List<string>();

                                foreach (var app in product.Applications)
                                {
                                    if (parentIds.Contains(app.ApplicationId))
                                        continue;

                                    string fullPath = CategoriesHelper.GetCategoryFullPath(app, allApplications);

                                    if (!writtenPaths.Contains(fullPath))
                                    {
                                        applicationPaths.Add(fullPath);
                                        writtenPaths.Add(fullPath);
                                    }
                                }

                                if (applicationPaths.Any())
                                {
                                    descriptionBuilder.Append("<p><b>Zastosowanie: </b>");
                                    descriptionBuilder.Append(string.Join(", ", applicationPaths));
                                    descriptionBuilder.Append("</p>");
                                }
                            }

                            WriteRawElement(writer, "Description", descriptionBuilder.ToString());

                            // Images
                            if (product.Images != null && product.Images.Any())
                            {
                                writer.WriteStartElement("Images");
                                foreach (var img in product.Images)
                                {
                                    writer.WriteStartElement("Image");
                                    WriteRawElement(writer, "ImageTitle", img.Title);
                                    WriteRawElement(writer, "ImageUrl", img.Url);
                                    writer.WriteEndElement(); // Image
                                }
                                writer.WriteEndElement(); // Images
                            }

                            if (product.Categories != null && product.Categories.Any())
                            {
                                // Build a dictionary of all categories
                                var allCategories = product.Categories
                                    .GroupBy(c => c.CategoryId)
                                    .ToDictionary(g => g.Key, g => g.First());

                                // Get a set of all parent IDs (these are not leaf nodes)
                                var parentIds = new HashSet<int>(product.Categories
                                    .Where(c => c.ParentID != 0)
                                    .Select(c => c.ParentID));

                                writer.WriteStartElement("Categories");

                                var writtenPaths = new HashSet<string>();

                                foreach (var cat in product.Categories)
                                {
                                    // Skip if it's a parent of any other category — we only want leaf nodes
                                    if (parentIds.Contains(cat.CategoryId))
                                        continue;

                                    string fullPath = CategoriesHelper.GetCategoryFullPath(cat, allCategories);

                                    if (!writtenPaths.Contains(fullPath))
                                    {
                                        WriteRawElement(writer, "Category", fullPath);
                                        writtenPaths.Add(fullPath);
                                    }
                                }

                                writer.WriteEndElement(); // Categories
                            }

                            // Packages
                            if (product.Packages != null && product.Packages.Any())
                            {
                                writer.WriteStartElement("Packages");
                                foreach (var pack in product.Packages)
                                {
                                    writer.WriteStartElement("Package");
                                    WriteRawElement(writer, "PackUnit", pack.PackUnit);
                                    WriteRawElement(writer, "PackQty", pack.PackQty.ToString());
                                    WriteRawElement(writer, "PackNettWeight", pack.PackNettWeight.ToString());
                                    WriteRawElement(writer, "PackGrossWeight", pack.PackGrossWeight.ToString());
                                    WriteRawElement(writer, "PackEan", pack.PackEan);
                                    WriteRawElement(writer, "PackRequired", pack.PackRequired.ToString());
                                    writer.WriteEndElement(); // Package
                                }
                                writer.WriteEndElement(); // Packages
                            }

                            // Components
                            if (product.Components != null && product.Components.Any())
                            {
                                writer.WriteStartElement("Components");
                                foreach (var comp in product.Components)
                                {
                                    writer.WriteStartElement("Component");
                                    WriteRawElement(writer, "TwrID", comp.TwrID.ToString());
                                    WriteRawElement(writer, "CodeGaska", comp.CodeGaska);
                                    WriteRawElement(writer, "Qty", comp.Qty.ToString());
                                    writer.WriteEndElement(); // Component
                                }
                                writer.WriteEndElement(); // Components
                            }

                            // Parameters
                            if (product.Parameters != null && product.Parameters.Any())
                            {
                                writer.WriteStartElement("Parameters");
                                foreach (var param in product.Parameters)
                                {
                                    writer.WriteStartElement("Parameter");
                                    WriteRawElement(writer, "AttributeName", param.AttributeName);
                                    WriteRawElement(writer, "AttributeValue", param.AttributeValue);
                                    writer.WriteEndElement(); // Parameter
                                }
                                writer.WriteEndElement(); // Parameters
                            }

                            // Applications
                            if (product.Applications != null && product.Applications.Any())
                            {
                                // Build a dictionary of all applications
                                var allApplications = product.Applications
                                    .GroupBy(a => a.ApplicationId)
                                    .ToDictionary(g => g.Key, g => g.First());

                                var parentIds = new HashSet<int>(product.Applications
                                                                  .Where(c => c.ParentID != 0)
                                                                  .Select(c => c.ParentID));

                                writer.WriteStartElement("Applications");

                                var writtenPaths = new HashSet<string>();

                                foreach (var app in product.Applications)
                                {
                                    if (parentIds.Contains(app.ApplicationId))
                                        continue;

                                    string fullPath = CategoriesHelper.GetCategoryFullPath(app, allApplications);

                                    if (!writtenPaths.Contains(fullPath))
                                    {
                                        WriteRawElement(writer, "Application", fullPath);
                                        writtenPaths.Add(fullPath);
                                    }
                                }

                                writer.WriteEndElement(); // Applications
                            }

                            // RecommendedParts
                            if (product.RecommendedParts != null && product.RecommendedParts.Any())
                            {
                                writer.WriteStartElement("RecommendedParts");
                                foreach (var rec in product.RecommendedParts)
                                {
                                    writer.WriteStartElement("RecommendedPart");
                                    WriteRawElement(writer, "TwrId", rec.TwrID.ToString());
                                    WriteRawElement(writer, "CodeGaska", rec.CodeGaska);
                                    WriteRawElement(writer, "Qty", rec.Qty.ToString());
                                    writer.WriteEndElement(); // RecommendedPart
                                }
                                writer.WriteEndElement(); // RecommendedParts
                            }

                            writer.WriteEndElement(); // Close Product
                        }
                        writer.WriteEndElement(); // Close Products
                    }

                    Log.Information($"Generated XML file in {resultFilePath}");
                }
                else
                {
                    Log.Warning("Database is empty. No products to send.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while making XML file."); ;
            }

            return resultFilePath;
        }

        public async Task UploadFileToFtp(string localFilePath)
        {
            try
            {
                string ftpUri = $"ftp://{_ftpSettings.Ip}:{_ftpSettings.Port}/products.xml";
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUri);
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential(_ftpSettings.Username, _ftpSettings.Password);

                byte[] fileContents = File.ReadAllBytes(localFilePath);
                request.ContentLength = fileContents.Length;

                using (Stream requestStream = await request.GetRequestStreamAsync())
                {
                    await requestStream.WriteAsync(fileContents, 0, fileContents.Length);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error uploading file to FTP.");
            }
        }

        private static void WriteRawElement(XmlWriter writer, string elementName, string value)
        {
            string sanitizedValue = RemoveInvalidXmlChars(value);
            writer.WriteStartElement(elementName);
            writer.WriteRaw($"<![CDATA[{sanitizedValue}]]>");
            writer.WriteEndElement();
        }

        private static string RemoveInvalidXmlChars(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            return new string(text.Where(c =>
                c == '\t' || c == '\n' || c == '\r' ||
                (c >= 32 && c <= 0xD7FF) ||
                (c >= 0xE000 && c <= 0xFFFD) ||
                (c >= 0x10000 && c <= 0x10FFFF)
            ).ToArray());
        }

        private void CleanupOldXmlFiles()
        {
            try
            {
                string resultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "result");

                if (!Directory.Exists(resultPath))
                    return;

                var files = Directory.GetFiles(resultPath, "*.xml");

                foreach (var file in files)
                {
                    var creationTime = File.GetCreationTime(file);
                    if ((DateTime.Now - creationTime).TotalDays > 1)
                    {
                        File.Delete(file);
                        Log.Information($"Deleted old XML file: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during cleanup of old XML files.");
            }
        }
    }
}