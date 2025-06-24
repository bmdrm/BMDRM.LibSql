# BMDRM.LibSQL.Core

**EF Core Provider for LibSQL — Maintained by BMDRM**

[![Build Status](https://img.shields.io/azure-devops/build/dnceng-public/public/17/main)](https://dev.azure.com/dnceng-public/public/_build?definitionId=17)
[![NuGet](https://img.shields.io/nuget/v/BMDRM.LibSql.Core?label=BMDRM.LibSql.Core)](https://www.nuget.org/packages/BMDRM.LibSql.Core)

BMDRM.LibSQL.Core is an Entity Framework Core provider for [LibSQL](https://libsql.org/), developed and maintained by BMDRM. It is designed for performance, reliability, and production-grade compatibility with EF Core 9.0.

> Licensed under the [MIT License](LICENSE.txt).

---

## Features

* ✅ **EF Core 9 Compatibility**

    * LINQ-to-SQL translation
    * Change tracking
    * Migration support

* ⚙️ **LibSQL-Specific Enhancements**

    * Optimized HTTP/2 connection pooling
    * JWT-based authentication
    * Distributed transaction support

* 🧪 **Production-Tested**

    * Stable under high concurrency
    * Compatible with CI/CD workflows

---

## Getting Started
[BMDRM.LibSQL.Core.csproj](BMDRM.LibSQL.Core.csproj)[BMDRM.LibSQL.Core.csproj](BMDRM.LibSQL.Core.csproj)
### 1. Install Packages

```bash
dotnet add package BMDRM.LibSql.Core --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.7
dotnet add package Microsoft.EntityFrameworkCore.Relational --version 9.0.7
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.7
```

> **Note:** Use version `9.0.7` for EF Core packages to ensure compatibility. Support for EF Core 9 will be added in a future release.

### 2. Configure Your Context

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseLibSql(config.GetConnectionString("LibSQL")));
```

### 3. Apply Migrations

```bash
dotnet ef database update
```

---

## Project Structure

```
/src/EFCore.LibSQL.Core
├── /Connection           # HTTP/2 connection management
├── /Security             # JWT/NKey authentication handling
└── /BMDRM.Extensions     # Additional performance enhancements
```

---

## Usage Example

```csharp
// Standard EF Core usage
var users = await db.Users
    .Where(u => u.Region == regionId)
    .ToListAsync();
```

---

## Support

* [GitHub Issues](https://github.com/bmdrm/efcore-libsql-core/issues)
* Email: [support@bmdrm.dev](mailto:support@bmdrm.dev)
* Status: [status.bmdrm.dev](https://status.bmdrm.dev)

---

## License & Contribution

* **License:** MIT
* **Maintainers:** BMDRM Core Team
* **Contributions:** All pull requests are reviewed
* **Build from Source:**

```bash
git clone https://github.com/bmdrm/BMDRM.LibSql.git
./build.sh --use-hardened
```

---

## Resources

* [Documentation](https://libsql.bmdrm.dev)
* [Community Forum](https://forum.bmdrm.dev)
* [Service Status](https://status.bmdrm.dev)

---

If you'd like, I can also provide a markdown file with this cleaned-up content, or tailor the tone further for enterprise clients or developers.
