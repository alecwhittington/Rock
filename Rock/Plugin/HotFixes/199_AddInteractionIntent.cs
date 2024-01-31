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

namespace Rock.Plugin.HotFixes
{
    /// <summary>
    /// Plug-in migration
    /// </summary>
    /// <seealso cref="Rock.Plugin.Migration" />
    [MigrationNumber( 199, "1.6.2" )]
    public class AddInteractionIntent : Migration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            JPH_AddInteractionIntent();
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            // Down migrations are not yet supported in plug-in migrations.
        }

        /// <summary>
        /// JPH: Add Interaction Intent defined type and defined values.
        /// </summary>
        private void JPH_AddInteractionIntent()
        {
            RockMigrationHelper.AddDefinedType( "Intents", "Interaction Intent", "Describes the purpose of an Interaction.", Rock.SystemGuid.DefinedType.INTERACTION_INTENT );

            Sql( $@"
UPDATE [DefinedType]
SET [CategorizedValuesEnabled] = 1
WHERE [Guid] = '{Rock.SystemGuid.DefinedType.INTERACTION_INTENT}';" );

            RockMigrationHelper.UpdateDefinedValue( Rock.SystemGuid.DefinedType.INTERACTION_INTENT, "Discipleship", "Describes an Interaction as having a discipleship purpose.", "D5CCB505-BCD7-4B90-98C9-108741E90C63", isSystem: false );
            RockMigrationHelper.UpdateDefinedValue( Rock.SystemGuid.DefinedType.INTERACTION_INTENT, "Youth Interest", "Describes an Interaction as having a youth interest purpose.", "C2452E2A-3DCD-45CD-A75B-FA7FA9DF4D3A", isSystem: false );
        }
    }
}
