using System.Reflection;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rock.Tests.Shared.TestFramework
{
    /// <summary>
    /// All unit tests that require database access should inherit from
    /// this class. It will abstract how away how a database is provided
    /// to each test. Currently this is done with docker images.
    /// </summary>
    public abstract class DatabaseTestsBase
    {
        /// <summary>
        /// <c>true</c> if we need to destroy the test container after the
        /// current test has finished.
        /// </summary>
        private bool _testWantsIsolatedDatabase = false;

        /// <summary>
        /// The current container providing the database.
        /// </summary>
        private static TestDatabaseContainer _container;

        /// <summary>
        /// Set by the test framework to indicate which test is running.
        /// </summary>
        public TestContext TestContext { get; set; }

        #region Methods

        /// <summary>
        /// Called before any test is executed in the entire class.
        /// </summary>
        /// <param name="context">The context of the first test that will be run.</param>
        /// <returns>A task that indicates when the operation has completed.</returns>
        [ClassInitialize( InheritanceBehavior.BeforeEachDerivedClass )]
        public static Task ContainerClassInitialize( TestContext context )
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called after every test in the class has finished executing.
        /// </summary>
        /// <returns>A task that indicates when the cleanup has completed.</returns>
        [ClassCleanup( InheritanceBehavior.BeforeEachDerivedClass )]
        public static async Task ContainerClassCleanup()
        {
            // Make sure we shut down the container at the end of all tests
            // in this class.
            if ( _container != null )
            {
                try
                {
                    await _container.DisposeAsync();
                }
                finally
                {
                    _container = null;
                }
            }
        }

        /// <summary>
        /// Called before each test in the class is executed. This will
        /// also be called before each data row of the test is executed.
        /// So if a test has 20 data rows and requests a new database, we
        /// will spin up a new database 20 times.
        /// </summary>
        /// <returns>A task that indicates when the initialization has completed.</returns>
        [TestInitialize]
        public async Task ContainerTestInitialize()
        {
            var method = GetType().GetMethod( TestContext.TestName );

            _testWantsIsolatedDatabase = method.GetCustomAttribute<IsolatedTestDatabaseAttribute>() != null
                || GetType().GetCustomAttribute<IsolatedTestDatabaseAttribute>() != null;

            if ( _container == null || _testWantsIsolatedDatabase )
            {
                if ( _container != null )
                {
                    try
                    {
                        await _container.DisposeAsync();
                    }
                    finally
                    {
                        _container = null;
                    }
                }

                var container = new TestDatabaseContainer();

                await container.StartAsync();

                _container = container;
            }
        }

        /// <summary>
        /// Called after each test in the class has completed.
        /// </summary>
        /// <returns>A task that indicates when cleanup is completed.</returns>
        [TestCleanup]
        public async Task ContainerTestCleanup()
        {
            // If the test indicated that it needed an isolated database
            // then shut it down so the next test gets a fresh one.
            if ( _testWantsIsolatedDatabase && _container != null )
            {
                try
                {
                    await _container.DisposeAsync();
                }
                finally
                {
                    _container = null;
                }
            }
        }

        #endregion
    }
}
