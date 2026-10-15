# Jahhezni - Full-Stack Business & Payments Platform

A multi-stakeholder marketplace that brings product browsing, budget planning, multi-vendor ordering, and service booking (halls, photographers, videographers) into one unified system.

Graduation project at Assiut University, Business Information Systems (BIS). Grade: A+.

## Features

- Role-based dashboards and access control for users, vendors, service providers, and admins
- Budget planning for users
- Multi-vendor ordering
- Service booking
- Secure online payments across multiple vendors using the Paymob payment gateway, with transaction processing and reconciliation logic

## Tech Stack

- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- Razor Views, HTML, CSS, JavaScript, Bootstrap
- Paymob API

## Getting Started

1. Clone the repository.
2. Open `Jahhezni.csproj` in Visual Studio.
3. In `appsettings.json`, replace the placeholder values (`YOUR_PAYMOB_API_KEY`, `YOUR_INTEGRATION_ID`, `YOUR_IFRAME_ID`) with your own credentials.
4. Make sure SQL Server LocalDB is installed, then run this command in the Package Manager Console to create the database:

```
Update-Database
```

5. Press F5 to run the project.

## Note

API keys and credentials have been removed from this repository for security.

## Author

Thomas Mikhail Ishak
LinkedIn: https://www.linkedin.com/in/tomas-m-ishak

Copyright 2026 Thomas Mikhail Ishak. All rights reserved.
