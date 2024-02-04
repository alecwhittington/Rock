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
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rock.Tests.Integration;

namespace Rock.Tests.Performance
{
    [TestClass]
    public sealed class PerformanceTestInitializer
    {
        /// <summary>
        /// This will run before any tests in this assembly are run.
        /// </summary>
        /// <param name="context">The context.</param>
        [AssemblyInitialize]
        public static async Task AssemblyInitialize( TestContext context )
        {
            //IsContainersEnabled = ConfigurationManager.ConnectionStrings["RockContext"] == null;

            await IntegrationTestInitializer.InitializeTestEnvironment( context );
        }
    }
}
