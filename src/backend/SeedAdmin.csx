#!/usr/bin/env dotnet-script
#r "nuget: BCrypt.Net-Next, 4.0.3"

using BCrypt.Net;

var email = "admin@learna.com";
var password = "Admin123!";
var passwordHash = BCrypt.HashPassword(password);
var createdAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

Console.WriteLine("-- Admin User Seed Script");
Console.WriteLine($"-- Password: {password}");
Console.WriteLine();
Console.WriteLine("USE learna_dev;");
Console.WriteLine();
Console.WriteLine("-- Insert Admin User");
Console.WriteLine($"INSERT INTO Users (Email, PasswordHash, CreatedAt, IsActive)");
Console.WriteLine($"VALUES ('{email}', '{passwordHash}', '{createdAt}', 1);");
Console.WriteLine();
Console.WriteLine("-- Get the User ID");
Console.WriteLine("SET @userId = LAST_INSERT_ID();");
Console.WriteLine();
Console.WriteLine("-- Assign Admin Role (Role ID 1 is Admin)");
Console.WriteLine("INSERT INTO UserRoles (UserId, RoleId, AssignedAt)");
Console.WriteLine($"VALUES (@userId, 1, '{createdAt}');");
Console.WriteLine();
Console.WriteLine("-- Verify");
Console.WriteLine("SELECT u.Id, u.Email, r.Name as Role");
Console.WriteLine("FROM Users u");
Console.WriteLine("JOIN UserRoles ur ON u.Id = ur.UserId");
Console.WriteLine("JOIN Roles r ON ur.RoleId = r.Id");
Console.WriteLine($"WHERE u.Email = '{email}';");
