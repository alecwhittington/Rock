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
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Rock.Data;
using Rock.Model;

namespace Rock.Lava
{
    internal static partial class LavaFilters
    {
        /// <summary>
        /// Gets the set of personalization items that are relevant to the specified person.
        /// </summary>
        /// <param name="context">The Lava context.</param>
        /// <param name="input">The filter input.</param>
        /// <param name="entityTypeId"></param>
        /// <param name="name"></param>
        /// <param name="expireDateTime"></param>
        /// <param name="entitySetPurposeValueId"></param>
        /// <param name="note"></param>
        /// <param name="parentEntitySetId"></param>
        /// <returns>The value of the user preference.</returns>
        public static EntitySet CreateEntitySet( ILavaRenderContext context, object input, string name, string entityTypeId = null, string expireDateTime = null, string entitySetPurposeValueId = null, string note = null, string parentEntitySetId = null )
        {
            const int defaultExpiryInMinutes = 20;

            var internalEntityTypeId = InputParser.ConvertToIntegerOrDefault( entityTypeId, null, -1 );

            // Get the list of entity keys to include in the set.
            var entityIdList = new List<int>();

            bool isValid;
            if ( input is IEnumerable<int> inputIntList )
            {
                // Process the input as a collection of id values.
                entityIdList = inputIntList.ToList();
            }
            else if ( input is string inputString )
            {
                // Process the input as a collection of entity id values.
                isValid = InputParser.TryConvertToIntegerList( inputString, out entityIdList, "," );

                if ( !isValid )
                {
                    throw new Exception( "The entity identifier list is invalid." );
                }
            }
            else if ( input is IEnumerable<object> inputList )
            {
                var isEntityCollection = input.IsRockEntityCollection();
                if ( isEntityCollection )
                {
                    // Process as a collection of Rock Entities.
                    var entitiesList = inputList.Cast<IEntity>().Select( e => new { e.Id, e.TypeId } ).ToList();

                    if ( !internalEntityTypeId.HasValue )
                    {
                        // The first entity in the collection determines the type of the entity set.
                        internalEntityTypeId = entitiesList.Select( e => e.TypeId ).FirstOrDefault();
                    }

                    entityIdList = entitiesList.Select( e => e.Id ).ToList();
                }
                else
                {
                    // Process the input as a collection of entity id values.
                    isValid = InputParser.TryConvertToIntegerList( inputList, out entityIdList );

                    if ( !isValid )
                    {
                        throw new Exception( "The entity identifier list is invalid." );
                    }
                }
            }
            else 
            {
                throw new Exception( $"CreateEntitySet failed. The filter input must be a delimited list of key values or a collection of Rock Entities or keys. [InputType={input.GetType().Name}]" );
            }

            if ( !internalEntityTypeId.HasValue )
            {
                throw new Exception( "The Entity Type was not supplied or could not be determined from the input." );
            }

            int? expiryInMinutes = defaultExpiryInMinutes;
            if ( !string.IsNullOrWhiteSpace( expireDateTime ) )
            {
                expiryInMinutes = InputParser.ConvertToIntegerOrDefault( expireDateTime, defaultExpiryInMinutes );
                if ( expiryInMinutes == null )
                {
                    DateTimeOffset expiryDto;
                    var isValidDate = InputParser.TryConvertToDateTimeOffset( expireDateTime, out expiryDto );
                    if ( isValidDate )
                    {
                        expiryInMinutes = RockDateTime.Now.Subtract( expiryDto.DateTime ).TotalMinutes.ToIntSafe( defaultExpiryInMinutes );
                    }
                }
            }

            var rockContext = LavaHelper.GetRockContextFromLavaContext( context );

            var service = new EntitySetService( rockContext );

            var options = new AddEntitySetActionOptions
            {
                Name = name,
                Note = note,
                EntityTypeId = internalEntityTypeId.Value,
                ExpiryInMinutes = expiryInMinutes,
                EntityIdList = entityIdList,

                PurposeValueId = entitySetPurposeValueId.AsIntegerOrNull(),
                ParentEntitySetId = parentEntitySetId.AsIntegerOrNull()
            };

            var entitySetId = service.AddEntitySet( options );

            // Retrieve the entity set and return it.
            var entitySet = service.Queryable()
                .Include( s => s.Items )
                .FirstOrDefault( s => s.Id == entitySetId );

            return entitySet;
        }
    }
}
