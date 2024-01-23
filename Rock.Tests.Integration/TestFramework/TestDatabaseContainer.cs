// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

using System;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

using Docker.DotNet;
using Docker.DotNet.Models;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rock.Tests.Shared;

using Testcontainers.MsSql;

namespace Rock.Tests.Integration.TestFramework
{
    class TestDatabaseImageBuilder
    {
        public const string RepositoryName = "rockrms/tests-integration";

        public TestDatabaseImageBuilder()
        {
        }

        public async Task BuildAsync()
        {
            using ( var dockerClient = new DockerClientConfiguration().CreateClient() )
            {
                var container = new MsSqlBuilder()
                    .WithPortBinding( 31433, 1433 )
                    .WithPassword( "rock!Tests" )
                    .WithAutoRemove( false )
                    .WithCleanUp( false )
                    .Build();

                try
                {
                    await container.StartAsync();

                    await BuildContainerAsync( container );

                    await container.StopAsync();

                    await dockerClient.Images.CommitContainerChangesAsync( new CommitContainerChangesParameters
                    {
                        ContainerID = container.Id,
                        RepositoryName = RepositoryName,
                        Tag = GetTargetMigration()
                    } );
                }
                finally
                {
                    try
                    {
                        await container.StopAsync();
                    }
                    finally
                    {
                        try
                        {
                            await dockerClient.Containers.RemoveContainerAsync( container.Id, new ContainerRemoveParameters
                            {
                                Force = true
                            } );
                        }
                        catch ( Exception ex )
                        {
                            System.Diagnostics.Debug.WriteLine( ex );
                        }
                    }
                }
            }
        }

        private static async Task BuildContainerAsync( MsSqlContainer container )
        {
            var connectionString = container.GetConnectionString();
            var csb = new SqlConnectionStringBuilder( connectionString );

            using ( var connection = new SqlConnection( csb.ConnectionString ) )
            {
                await connection.OpenAsync();

                var rockConnectionString = ConfigurationManager.ConnectionStrings["RockContext"].ConnectionString;
                var dbName = new SqlConnectionStringBuilder( rockConnectionString ).InitialCatalog;

                await CreateDatabaseAsync( connection, dbName );

                MigrateDatabase( rockConnectionString );
            }
        }

        private static async Task CreateDatabaseAsync( SqlConnection connection, string dbName )
        {
            LogHelper.Log( $"Creating new database..." );

            using ( var cmd = connection.CreateCommand() )
            {
                cmd.CommandText = $@"
CREATE DATABASE [{dbName}];
ALTER DATABASE [{dbName}] SET RECOVERY SIMPLE";

                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Migrates the database.
        /// </summary>
        private static void MigrateDatabase( string connectionString )
        {
            var connection = new DbConnectionInfo( connectionString, "System.Data.SqlClient" );

            var config = new Rock.Migrations.Configuration
            {
                TargetDatabase = connection
            };

            var migrator = new System.Data.Entity.Migrations.DbMigrator( config );

            try
            {
                migrator.Update();
            }
            catch ( Exception ex )
            {
                throw new Exception( "Test Database migration failed. Verify that the database connection string specified in the test project is valid. You may need to manually synchronize the database or configure the test environment to force-create a new database.", ex );
            }
        }

        /// <summary>
        /// Gets the target migration.
        /// </summary>
        private static string GetTargetMigration()
        {
            return typeof( Rock.Migrations.RockMigration )
                .Assembly
                .GetExportedTypes()
                .Where( a => typeof( System.Data.Entity.Migrations.Infrastructure.IMigrationMetadata ).IsAssignableFrom( a ) )
                .Select( a => ( System.Data.Entity.Migrations.Infrastructure.IMigrationMetadata ) Activator.CreateInstance( a ) )
                .Select( a => a.Id.Substring( 0, 15 ) )
                .OrderByDescending( a => a )
                .First();
        }

        public static string GetRepositoryAndTag()
        {
            return $"{RepositoryName}:{GetTargetMigration()}";
        }

    }

    class TestDatabaseContainer
    {
        private static bool? _hasValidImage;

        private MsSqlContainer _databaseContainer;

        public TestDatabaseContainer()
        {
        }

        public async Task StartAsync()
        {
            if ( !( await HasValidImage() ) )
            {
                await new TestDatabaseImageBuilder().BuildAsync();
            }

            var container = new MsSqlBuilder()
                .WithImage( TestDatabaseImageBuilder.GetRepositoryAndTag() )
                .WithPortBinding( 31433 )
                .WithPassword( "rock!Tests" )
                .Build();

            await container.StartAsync();

            _databaseContainer = container;
        }

        public async Task DisposeAsync()
        {
            if ( _databaseContainer != null )
            {
                await _databaseContainer.DisposeAsync().AsTask();
            }
        }

        private async Task<bool> HasValidImage()
        {
            if ( _hasValidImage == null )
            {
                var repositoryTag = TestDatabaseImageBuilder.GetRepositoryAndTag();

                using ( var dockerClient = new DockerClientConfiguration().CreateClient() )
                {
                    var images = await dockerClient.Images.ListImagesAsync( new ImagesListParameters
                    {
                        All = true
                    } );

                    _hasValidImage = images.Any( i => i.RepoTags.Contains( repositoryTag ) );
                }
            }

            return _hasValidImage.Value;
        }
    }

    public abstract class DatabaseContainerTests
    {
        [ClassInitialize( InheritanceBehavior.BeforeEachDerivedClass )]
        public static Task ContainerClassInitialize( TestContext context )
        {
            System.Diagnostics.Debug.WriteLine( "ContainerClassInitialize" );
            return Task.CompletedTask;
        }

        [ClassCleanup( InheritanceBehavior.BeforeEachDerivedClass )]
        public static Task ContainerClassCleanup()
        {
            System.Diagnostics.Debug.WriteLine( "ContainerClassCleanup" );
            return Task.CompletedTask;
        }

        [TestInitialize]
        public Task ContainerTestInitialize()
        {
            System.Diagnostics.Debug.WriteLine( "ContainerTestInitialize" );
            return Task.CompletedTask;
        }

        [TestCleanup]
        public Task ContainerTestCleanup()
        {
            System.Diagnostics.Debug.WriteLine( "ContainerTestCleanup" );
            return Task.CompletedTask;
        }
    }
    [TestClass]
    public class Test1 : DatabaseContainerTests
    {
        private TestDatabaseContainer _container;

        [ClassInitialize]
        public static Task ClassInitialize( TestContext context )
        {
            System.Diagnostics.Debug.WriteLine( "ClassInitialize" );
            return Task.CompletedTask;
        }

        [ClassCleanup]
        public static Task ClassCleanup()
        {
            System.Diagnostics.Debug.WriteLine( "ClassCleanup" );
            return Task.CompletedTask;
        }

        [TestInitialize]
        public async Task TestInitialize()
        {
            System.Diagnostics.Debug.WriteLine( "TestInitialize" );
            var container = new TestDatabaseContainer();
            await container.StartAsync();

            _container = container;
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            System.Diagnostics.Debug.WriteLine( "TestCleanup" );

            if ( _container != null )
            {
                await _container.DisposeAsync();
            }
        }

        [TestMethod]
        public void DoTest()
        {
            System.Diagnostics.Debug.WriteLine( "DoTest" );
        }

        [TestMethod]
        public void DoTest2()
        {
            System.Diagnostics.Debug.WriteLine( "DoTest2" );
        }
    }
}
