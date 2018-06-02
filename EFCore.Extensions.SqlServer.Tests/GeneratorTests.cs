using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Microsoft.EntityFrameworkCore.SqlServerEndToEndTest;
using System;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.IO;
using System.Collections.Generic;

namespace EFCore.Extensions.SqlServer.Tests
{
    [TestClass]
    public class GeneratorTest : SqlServerFixture
    {

        private const string DatabaseName = "SqlServerEndToEndTest";

        [TestMethod]
        public async System.Threading.Tasks.Task Generate_Insert_For_Single_Entity()
        {
            using (var testDatabase = SqlServerTestStore.Create(DatabaseName))
            {
                var loggingFactory = new TestSqlLoggerFactory();
                var serviceProvider = new ServiceCollection()
                    .AddEntityFrameworkSqlServer()
                    .AddSingleton<ILoggerFactory>(loggingFactory)
                    .BuildServiceProvider();

                var optionsBuilder = new DbContextOptionsBuilder()
                    .EnableSensitiveDataLogging()
                    .UseSqlServer(testDatabase.ConnectionString, b => b.ApplyConfiguration())
                    .UseInternalServiceProvider(serviceProvider);

                using (var db = new BloggingContext(optionsBuilder.Options))
                {
                    await CreateBlogDatabaseAsync<Blog>(db);
                }

                loggingFactory.Clear();

                using (var db = new BloggingContext(optionsBuilder.Options))
                {
                   
                    var toAdd = db.Add(new Blog
                    {
                        Name = "Blog to Insert",
                        George = true,
                        TheGu = new Guid("0456AEF1-B7FC-47AA-8102-975D6BA3A9BF"),
                        NotFigTime = new DateTime(1973, 9, 3, 0, 10, 33, 777),
                        ToEat = 64,
                        OrNothing = 0.123456789,
                        Fuse = 777,
                        WayRound = 9876543210,
                        Away = 0.12345f,
                        AndChew = new byte[16]
                    }).Entity;

                    var statement = db.Generate();

                    var reader = new StringReader(statement);
                    IList<ParseError> errors;
                    var parser = new TSql140Parser(false);

                    var parseResult = parser.Parse(reader, out errors);

                    Assert.IsFalse(errors.Any());

                }
            }
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Generate_Update_For_Single_Entity()
        {

            using (var testDatabase = SqlServerTestStore.Create(DatabaseName))
            {
                var loggingFactory = new TestSqlLoggerFactory();
                var serviceProvider = new ServiceCollection()
                    .AddEntityFrameworkSqlServer()
                    .AddSingleton<ILoggerFactory>(loggingFactory)
                    .BuildServiceProvider();

                var optionsBuilder = new DbContextOptionsBuilder()
                    .EnableSensitiveDataLogging()
                    .UseSqlServer(testDatabase.ConnectionString, b => b.ApplyConfiguration())
                    .UseInternalServiceProvider(serviceProvider);

                using (var db = new BloggingContext(optionsBuilder.Options))
                {
                    await CreateBlogDatabaseAsync<Blog>(db);
                }

                loggingFactory.Clear();

                using (var db = new BloggingContext(optionsBuilder.Options))
                {
                    var toUpdate = db.Blogs.Single(b => b.Name == "Blog1");
                    toUpdate.Name = "Blog is Updated";
                    var updatedId = toUpdate.Id;
                   
                    db.Entry(toUpdate).State = EntityState.Modified;

                    var statement = db.Generate();

                    var reader = new StringReader(statement);
                    IList<ParseError> errors;
                    var parser = new TSql140Parser(false);

                    var parseResult = parser.Parse(reader, out errors);

                    Assert.IsFalse(errors.Any());
                }
            }
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Generate_Delete_For_Single_Entity()
        {

            using (var testDatabase = SqlServerTestStore.Create(DatabaseName))
            {
                var loggingFactory = new TestSqlLoggerFactory();
                var serviceProvider = new ServiceCollection()
                    .AddEntityFrameworkSqlServer()
                    .AddSingleton<ILoggerFactory>(loggingFactory)
                    .BuildServiceProvider();

                var optionsBuilder = new DbContextOptionsBuilder()
                    .EnableSensitiveDataLogging()
                    .UseSqlServer(testDatabase.ConnectionString, b => b.ApplyConfiguration())
                    .UseInternalServiceProvider(serviceProvider);

                using (var db = new BloggingContext(optionsBuilder.Options))
                {
                    await CreateBlogDatabaseAsync<Blog>(db);
                }

                loggingFactory.Clear();

                using (var db = new BloggingContext(optionsBuilder.Options))
                {
                   
                    var toDelete = db.Blogs.Single(b => b.Name == "Blog2");
                    toDelete.Name = "Blog to delete";
                    var deletedId = toDelete.Id;

                    
                    db.Entry(toDelete).State = EntityState.Deleted;

                    var statement = db.Generate();

                    var reader = new StringReader(statement);
                    IList<ParseError> errors;
                    var parser = new TSql140Parser(false);

                    var parseResult = parser.Parse(reader, out errors);

                    Assert.IsFalse(errors.Any());

                }
            }
        }

        [TestMethod]
        public async System.Threading.Tasks.Task Generate_For_DML()
        {
            
            using (var testDatabase = SqlServerTestStore.Create(DatabaseName))
            {
                var loggingFactory = new TestSqlLoggerFactory();
                var serviceProvider = new ServiceCollection()
                    .AddEntityFrameworkSqlServer()
                    .AddSingleton<ILoggerFactory>(loggingFactory)
                    .BuildServiceProvider();

                var optionsBuilder = new DbContextOptionsBuilder()
                    .EnableSensitiveDataLogging()
                    .UseSqlServer(testDatabase.ConnectionString, b => b.ApplyConfiguration())
                    .UseInternalServiceProvider(serviceProvider);

                using (var db = new BloggingContext(optionsBuilder.Options))
                {
                    await CreateBlogDatabaseAsync<Blog>(db);
                }

                loggingFactory.Clear();

                using (var db = new BloggingContext(optionsBuilder.Options))
                {
                    var toUpdate = db.Blogs.Single(b => b.Name == "Blog1");
                    toUpdate.Name = "Blog is Updated";
                    var updatedId = toUpdate.Id;
                    var toDelete = db.Blogs.Single(b => b.Name == "Blog2");
                    toDelete.Name = "Blog to delete";
                    var deletedId = toDelete.Id;

                    db.Entry(toUpdate).State = EntityState.Modified;
                    db.Entry(toDelete).State = EntityState.Deleted;

                    var toAdd = db.Add(new Blog
                    {
                        Name = "Blog to Insert",
                        George = true,
                        TheGu = new Guid("0456AEF1-B7FC-47AA-8102-975D6BA3A9BF"),
                        NotFigTime = new DateTime(1973, 9, 3, 0, 10, 33, 777),
                        ToEat = 64,
                        OrNothing = 0.123456789,
                        Fuse = 777,
                        WayRound = 9876543210,
                        Away = 0.12345f,
                        AndChew = new byte[16]
                    }).Entity;

                    var statement = db.Generate();

                    var reader = new StringReader(statement);
                    IList<ParseError> errors;
                    var parser = new TSql140Parser(false);

                    var parseResult = parser.Parse(reader, out errors);

                    Assert.IsFalse(errors.Any());

                    //var expectedStatement = "EXECUTE sp_executesql N'INSERT INTO Blog ([AndChew], [Away], [Fuse], [George], [Name], [NotFigTime], [OrNothing], [TheGu], [ToEat], [WayRound]) VALUES (@p0,@p1,@p2,@p3,@p4,@p5,@p6,@p7,@p8,@p9)', N'@p0 varbinary(max),@p1 real,@p2 smallint,@p3 bit,@p4 nvarchar(max),@p5 datetime2,@p6 float,@p7 uniqueidentifier,@p8 tinyint,@p9 bigint', @p0 = 0x00000000000000000000000000000000,@p1 = 0.12345,@p2 = 777,@p3 = True,@p4 = 'Blog to Insert',@p5 = '9/3/1973 12:10:33 AM',@p6 = 0.123456789,@p7 = '0456aef1-b7fc-47aa-8102-975d6ba3a9bf',@p8 = 64,@p9 = 9876543210";

                    //Assert.AreEqual(expectedStatement, statement);

                }
            }
        }


        [TestMethod]
        public async System.Threading.Tasks.Task Generate_Insert_For_Multiple_Entities()
        {

            using (var testDatabase = SqlServerTestStore.Create(DatabaseName))
            {
                var loggingFactory = new TestSqlLoggerFactory();
                var serviceProvider = new ServiceCollection()
                    .AddEntityFrameworkSqlServer()
                    .AddSingleton<ILoggerFactory>(loggingFactory)
                    .BuildServiceProvider();

                var optionsBuilder = new DbContextOptionsBuilder()
                    .EnableSensitiveDataLogging()
                    .UseSqlServer(testDatabase.ConnectionString, b => b.ApplyConfiguration())
                    .UseInternalServiceProvider(serviceProvider);

                using (var db = new BloggingContext(optionsBuilder.Options))
                {
                    await CreateBlogDatabaseAsync<Blog>(db);
                }

                loggingFactory.Clear();

                using (var db = new BloggingContext(optionsBuilder.Options))
                {

                    var toAdd = db.Add(new Blog
                    {
                        Name = "Blog to Insert",
                        George = true,
                        TheGu = new Guid("0456AEF1-B7FC-47AA-8102-975D6BA3A9BF"),
                        NotFigTime = new DateTime(1973, 9, 3, 0, 10, 33, 777),
                        ToEat = 64,
                        OrNothing = 0.123456789,
                        Fuse = 777,
                        WayRound = 9876543210,
                        Away = 0.12345f,
                        AndChew = new byte[16]
                    }).Entity;

                    var recordTwo = db.Add(new Blog
                    {
                        Name = "Another Blog to Insert",
                        George = true,
                        TheGu = new Guid("0456AEF1-B7FC-47AA-8102-975D6BA3A9BE"),
                        NotFigTime = new DateTime(1974, 9, 3, 0, 10, 33, 777),
                        ToEat = 65,
                        OrNothing = 0.123456789,
                        Fuse = 777,
                        WayRound = 9876543210,
                        Away = 0.12345f,
                        AndChew = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
                    }).Entity;

                    var statement = db.Generate();

                    var reader = new StringReader(statement);
                    IList<ParseError> errors;
                    var parser = new TSql140Parser(false);

                    var parseResult = parser.Parse(reader, out errors);

                    Assert.IsFalse(errors.Any());

                }
            }
        }
    }
}
