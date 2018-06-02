# EFCore.Extensions

EFCore.Extensions is intended to be a set of useful extensions to [Entity Framework Core].

# Current Features

- Generate T-SQL DML statements from the Entity Framework change tracker for Microsoft Sql Server

### Roadmap:

- Add more comprehensive testing
- Add support for streams so statements can be stored to files more readily
- Add support for addional databases
- Add Bulk capabilities
- Add an extension method to obtain sql query statements

### Notes:

***This is alpha software***
**Currently it is only tested as working with Entity Framework Core 2.0.3**
Entity Framework Core 2.1.0 has changes that will require this code to be modified in order to work. Specifically, SqlServerTypeMapper has been removed

***If you're looking for a .ToSql() extension, one was posted by [Smit Patel] on [Issue 9414]***

### How To Use:
Use intructions will be provided at a later date. For now review the Unit Tests in ***GeneratorTests.cs***

### Unit Tests:

Unit tests were created with MSTest vs xUnit due to an issue with the xUnit test runner in some versions of Visual Studio.  
The issue was corrected as of Visual Studio 15.7.3 but the tests have yet to be switched to the xUnit framework. It is intended that all tests will be migrated to xUnit in the future

- All Sql Server Data Types have not been tested
- Several scenarios still need to be tested
- Tests borrow directly from the Entity Framework Core Test Projects

### License
Apache 2.0

[Entity Framework Core]: https://github.com/aspnet/
[Smit Patel]: https://github.com/smitpatel
[Issue 9414]: https://github.com/aspnet/EntityFrameworkCore/issues/9414


