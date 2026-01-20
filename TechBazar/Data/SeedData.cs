using Microsoft.AspNetCore.Identity;
using TechBazar.Models;

namespace TechBazar.Data
{
    public static class SeedData
    {
        public static void Initialize(ApplicationDbContext context)
        {
            // Seed categories if none exist
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { Name = "Laptops", Description = "Portable computers" },
                    new Category { Name = "Smartphones", Description = "Mobile phones" },
                    new Category { Name = "Tablets", Description = "Tablet computers" },
                    new Category { Name = "Accessories", Description = "Computer accessories" }
                );
                context.SaveChanges();
            }

            // Seed products if none exist
            if (!context.Products.Any())
            {
                var laptopsCategory = context.Categories.FirstOrDefault(c => c.Name == "Laptops");
                var smartphonesCategory = context.Categories.FirstOrDefault(c => c.Name == "Smartphones");
                var tabletsCategory = context.Categories.FirstOrDefault(c => c.Name == "Tablets");
                var accessoriesCategory = context.Categories.FirstOrDefault(c => c.Name == "Accessories");

                var productsToAdd = new List<Product>
                {
                    new Product
                    {
                        Name = "Dell XPS 13",
                        Description = "Ultra-portable laptop with stunning display",
                        Price = 1299.99m,
                        StockQuantity = 10,
                        CategoryId = laptopsCategory?.Id ?? 1,
                        ImageUrl = "/images/dell-xps.jpg",
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Samsung Galaxy S24",
                        Description = "Latest smartphone with advanced camera",
                        Price = 899.99m,
                        StockQuantity = 15,
                        CategoryId = smartphonesCategory?.Id ?? 2,
                        ImageUrl = "/images/galaxy-s24.jpg",
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "iPad Air",
                        Description = "Versatile tablet for work and play",
                        Price = 599.99m,
                        StockQuantity = 8,
                        CategoryId = tabletsCategory?.Id ?? 3,
                        ImageUrl = "/images/ipad-air.jpg",
                        IsActive = true
                    },
                    new Product
                    {
                        Name = "Wireless Earbuds",
                        Description = "High-quality wireless earbuds",
                        Price = 149.99m,
                        StockQuantity = 20,
                        CategoryId = accessoriesCategory?.Id ?? 4,
                        ImageUrl = "/images/earbuds.jpg",
                        IsActive = true
                    }
                };

                context.Products.AddRange(productsToAdd);
                context.SaveChanges();

                // Seed translations for products using actual IDs
                SeedProductTranslations(context, productsToAdd);
            }

            // Seed permissions - remove old and add new
            var oldPermissions = context.Permissions.Where(p => p.Name == "Products.Manage").ToList();
            foreach (var oldPerm in oldPermissions)
            {
                context.Permissions.Remove(oldPerm);
            }
            context.SaveChanges();

            var permissionsToAdd = new List<Permission>
            {
                new Permission { Name = "ManageProducts", Description = "Can manage products" },
                new Permission { Name = "Products.Create", Description = "Can create products" },
                new Permission { Name = "Products.Update", Description = "Can update products" },
                new Permission { Name = "Products.Delete", Description = "Can delete products" },
                new Permission { Name = "ManageUsers", Description = "Can manage user accounts" },
                new Permission { Name = "ViewReports", Description = "Can view sales reports" }
            };

            foreach (var permission in permissionsToAdd)
            {
                if (!context.Permissions.Any(p => p.Name == permission.Name))
                {
                    context.Permissions.Add(permission);
                }
            }
            context.SaveChanges();

            // Seed role permissions
            var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
            var managerRole = context.Roles.FirstOrDefault(r => r.Name == "Manager");

            if (adminRole != null)
            {
                var permissions = context.Permissions.ToList();
                foreach (var permission in permissions)
                {
                    var existing = context.RolePermissions.FirstOrDefault(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id);
                    if (existing == null)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = adminRole.Id,
                            PermissionId = permission.Id,
                            Role = adminRole,
                            Permission = permission
                        });
                    }
                }
            }

            if (managerRole != null)
            {
                // Assign Products.Create and Products.Update permissions to Manager role (restrict delete)
                var createProductsPermission = context.Permissions.FirstOrDefault(p => p.Name == "Products.Create");
                var updateProductsPermission = context.Permissions.FirstOrDefault(p => p.Name == "Products.Update");

                if (createProductsPermission != null)
                {
                    var existing = context.RolePermissions.FirstOrDefault(rp => rp.RoleId == managerRole.Id && rp.PermissionId == createProductsPermission.Id);
                    if (existing == null)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = managerRole.Id,
                            PermissionId = createProductsPermission.Id,
                            Role = managerRole,
                            Permission = createProductsPermission
                        });
                    }
                }

                if (updateProductsPermission != null)
                {
                    var existing = context.RolePermissions.FirstOrDefault(rp => rp.RoleId == managerRole.Id && rp.PermissionId == updateProductsPermission.Id);
                    if (existing == null)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = managerRole.Id,
                            PermissionId = updateProductsPermission.Id,
                            Role = managerRole,
                            Permission = updateProductsPermission
                        });
                    }
                }

                // Remove delete permission from manager if it exists
                var deleteProductsPermission = context.Permissions.FirstOrDefault(p => p.Name == "Products.Delete");
                if (deleteProductsPermission != null)
                {
                    var existingDelete = context.RolePermissions.FirstOrDefault(rp => rp.RoleId == managerRole.Id && rp.PermissionId == deleteProductsPermission.Id);
                    if (existingDelete != null)
                    {
                        context.RolePermissions.Remove(existingDelete);
                    }
                }
            }

            context.SaveChanges();

            // Seed translations for categories and products
            //SeedTranslations(context);
        }

        private static void SeedProductTranslations(ApplicationDbContext context, List<Product> products)
        {
            if (!context.MultiLanguageTranslators.Any(t => t.TableName == "Product"))
            {
                var translations = new List<MultiLanguageTranslator>();

                foreach (var product in products)
                {
                    // English translations
                    translations.Add(new MultiLanguageTranslator
                    {
                        LanguageId = 1,
                        TableName = "Product",
                        ColumnName = "Name",
                        EntityId = product.Id,
                        TranslationValue = product.Name
                    });
                    translations.Add(new MultiLanguageTranslator
                    {
                        LanguageId = 1,
                        TableName = "Product",
                        ColumnName = "Description",
                        EntityId = product.Id,
                        TranslationValue = product.Description
                    });

                    // Bangla translations (simplified)
                    translations.Add(new MultiLanguageTranslator
                    {
                        LanguageId = 2,
                        TableName = "Product",
                        ColumnName = "Name",
                        EntityId = product.Id,
                        TranslationValue = product.Name // For now, use English as placeholder
                    });
                    translations.Add(new MultiLanguageTranslator
                    {
                        LanguageId = 2,
                        TableName = "Product",
                        ColumnName = "Description",
                        EntityId = product.Id,
                        TranslationValue = product.Description // For now, use English as placeholder
                    });
                }

                context.MultiLanguageTranslators.AddRange(translations);
                context.SaveChanges();
            }
        }


    }
}
