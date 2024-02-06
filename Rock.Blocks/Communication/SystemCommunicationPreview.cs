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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;

using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.ViewModels.Blocks.Communication.SystemCommunicationPreview;
using Rock.Web.Cache;

namespace Rock.Blocks.Communication
{
    [DisplayName( "System Communication Preview" )]
    [Category( "Communication" )]
    [Description( "Create a preview and send a test message for the given system communication using the selected date and target person." )]
    // [SupportedSiteTypes( Model.SiteType.Web )]

    #region Block Attributes

    [SystemCommunicationField( "System Communication",
         Description = "The system communication to use when previewing the message. When set as a block setting, it will not allow overriding by the query string.",
         IsRequired = false,
         Order = 0,
         Key = AttributeKey.SystemCommunication )]

    [DaysOfWeekField( "Send Day of the Week",
         Description = "Used to determine which dates to list in the Message Date drop down. <i><strong>Note:</strong> If no day is selected the Message Date drop down will not be shown and the ‘SendDateTime’ Lava variable will be set to the current day.</i>",
         IsRequired = false,
         Order = 1,
         Key = AttributeKey.SendDaysOfTheWeek )]

    [IntegerField( "Number of Previous Weeks to Show",
         Description = "How many previous weeks to show in the drop down.",
         DefaultIntegerValue = 6,
         Order = 3,
         Key = AttributeKey.PreviousWeeksToShow )]

    [IntegerField( "Number of Future Weeks to Show",
         Description = "How many weeks ahead to show in the drop down.",
         DefaultIntegerValue = 1,
         Order = 4,
         Key = AttributeKey.FutureWeeksToShow )]

    [LavaCommandsField( "Enabled Lava Commands",
         Description = "The Lava commands that should be enabled.",
         IsRequired = false,
         Key = AttributeKey.EnabledLavaCommands,
         Order = 5 )]

    [CodeEditorField( "Lava Template Append",
         Description = "This Lava will be appended to the system communication template to help setup any data that the template needs. This data would typically be passed to the template by a job or other means.",
         DefaultValue = "",
         IsRequired = false,
         Key = AttributeKey.LavaTemplateAppend,
         Order = 6 )]

    #endregion Block Attributes

    [Rock.SystemGuid.BlockTypeGuid( "C28368CA-5218-4B59-8BD8-75BD78AA9BE9" )]
    public class SystemCommunicationPreview : RockBlockType
    {
        #region Fields

        internal bool HasSendDate { get; set; }
        internal bool HasSystemCommunication = false;
        internal bool HasTargetPerson = false;

        #endregion

        #region Page Constants

        private static class PageConstants
        {
            public const string LavaDebugCommand = "{{ 'Lava' | Debug }}";
        }

        #endregion

        #region Page Parameter Keys

        private static class PageParameterKey
        {
            public const string SystemCommunicationId = "SystemCommunicationId";
            public const string PublicationDate = "PublicationDate";
            public const string TargetPersonId = "TargetPersonId";
        }

        #endregion Page Parameter Keys

        #region Attribute Keys

        private static class AttributeKey
        {
            public const string SystemCommunication = "SystemCommunication";
            public const string SendDaysOfTheWeek = "SendDaysOfTheWeek";
            public const string PreviousWeeksToShow = "PreviousWeeksToShow";
            public const string FutureWeeksToShow = "FutureWeeksToShow";
            public const string EnabledLavaCommands = "EnabledLavaCommands";
            public const string LavaTemplateAppend = "LavaTemplateAppend";
        }

        #endregion Attribute Keys

        #region Merge Field Keys

        private static class MergeFieldKey
        {
            public const string SendDateTime = "SendDateTime";
            public const string Person = "Person";
        }

        #endregion Merge Field Keys

        #region ViewState Keys

        private static class ViewStateKey
        {
            public const string SystemCommunicationGuid = "SystemCommunicationGuid";
            public const string TargetPersonId = "TargetPersonId";
            public const string SelectedDate = "SelectedDate";
        }

        #endregion ViewState Keys

        #region Properties

        protected string EnabledLavaCommands => GetAttributeValue( AttributeKey.EnabledLavaCommands );

        #endregion

        #region Methods

        public override object GetObsidianBlockInitialization()
        {
            var rockContext = new RockContext();

            // Get System Communication Guid from Block Settings or QueryString.
            Guid? systemCommunicationGuid = GetAttributeValue( AttributeKey.SystemCommunication ).AsGuidOrNull();

            if ( systemCommunicationGuid == null )
            {
                var systemCommunicationId = RequestContext.GetPageParameter( PageParameterKey.SystemCommunicationId ).AsIntegerOrNull();
                if ( systemCommunicationId.HasValue )
                {
                    systemCommunicationGuid = new SystemCommunicationService( rockContext ).GetGuid( systemCommunicationId.Value );
                }
            }

            var systemCommunicationService = new SystemCommunicationService( rockContext );
            var systemCommunication = systemCommunicationService.Get( systemCommunicationGuid.Value );

            if ( systemCommunication != null )
            {
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, null );
                string bodyHtml = systemCommunication.Body.ResolveMergeFields( mergeFields );
                string subjectHtml = systemCommunication.Subject.ResolveMergeFields( mergeFields );
                var hasSendDate = systemCommunication.Body.Contains( "{{ SendDateTime }}" );

                DateTime publicationDate = DateTime.Now;

                var systemCommunicationPreviewBag = new SystemCommunicationPreviewBag
                {

                    Title = systemCommunication.Title,
                    From = systemCommunication.From,
                    FromName = systemCommunication.FromName.IsNullOrWhiteSpace() ? GlobalAttributesCache.Get().GetValue( "OrganizationName" ) : systemCommunication.FromName,
                    Subject = subjectHtml,
                    Body = bodyHtml,
                    Date = publicationDate.ToString( "MMMM d, yyy" ),
                    HasSendDate = hasSendDate,
                };

                return systemCommunicationPreviewBag;
            }

            return null;
        }

        private void SetEmailFromDetails( RockEmailMessage rockEmailMessage, SystemCommunication systemCommunication )
        {
            var globalAttributes = GlobalAttributesCache.Get();

            // Email - From Name
            if ( string.IsNullOrWhiteSpace( systemCommunication.FromName ) )
            {
                systemCommunication.FromName = globalAttributes.GetValue( "OrganizationName" );
            }

            rockEmailMessage.FromName = systemCommunication.FromName;

            // Email - From Address
            if ( string.IsNullOrWhiteSpace( systemCommunication.From ) )
            {
                systemCommunication.From = globalAttributes.GetValue( "OrganizationEmail" );
            }

            rockEmailMessage.FromEmail = systemCommunication.From;
        }

        #endregion

        #region Block Actions

        [BlockAction]
        public SystemCommunicationPreviewBag SetSystemCommunicationAsync( string systemCommunicationIdParam, int targetPersonId, string publicationDateParam )
        {
            var rockContext = new RockContext();
            var systemCommunicationService = new SystemCommunicationService( rockContext );
            SystemCommunication systemCommunication = null;

            var systemCommunicationGuid = GetAttributeValue( AttributeKey.SystemCommunication ).AsGuid();
            if ( !systemCommunicationGuid.IsEmpty() )
            {
                // Logic to fetch and return system communication based on GUID
                systemCommunication = systemCommunicationService.Get( systemCommunicationGuid );
            }
            else
            {
                // Logic to fetch and return system communication based on the query string param
                var systemCommunicationId = systemCommunicationIdParam.AsIntegerOrNull();
                if ( systemCommunicationId.HasValue )
                {
                    systemCommunication = systemCommunicationService.Get( systemCommunicationId.Value );
                }
            }

            if ( systemCommunication != null )
            {
                var rockEmailMessage = new RockEmailMessage( systemCommunicationGuid );
                var appendTemplate = GetAttributeValue( AttributeKey.LavaTemplateAppend );
                DateTime publicationDate;

                if ( !string.IsNullOrWhiteSpace( appendTemplate ) )
                {
                    rockEmailMessage.Message = appendTemplate + rockEmailMessage.Message;
                }

                if ( !DateTime.TryParse( publicationDateParam, out publicationDate ) )
                {
                    publicationDate = DateTime.Now;
                }

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, null );

                // Add custom merge field for Person if targetPersonId is valid
                if ( targetPersonId >= 0 )
                {
                    var person = new PersonService( rockContext ).Get( targetPersonId );
                    if ( person != null )
                    {
                        // Add the person as a merge field. Adjust the key as needed.
                        mergeFields.Add( "Person", person );
                    }
                }

                // Resolve merge fields and encode HTML
                string bodyHtml = systemCommunication.Body.ResolveMergeFields( mergeFields );
                string subjectHtml = systemCommunication.Subject.ResolveMergeFields( mergeFields );

                var systemCommunicationPreviewBag = new SystemCommunicationPreviewBag
                {
                    Id = systemCommunication.Id,
                    Title = systemCommunication.Title,
                    From = systemCommunication.From,
                    FromName = systemCommunication.FromName.IsNullOrWhiteSpace() ? GlobalAttributesCache.Get().GetValue( "OrganizationName" ) : systemCommunication.FromName,
                    Subject = subjectHtml,
                    Body = bodyHtml,
                    Date = publicationDate.ToString( "MMMM d, yyy" )
                };

                return systemCommunicationPreviewBag;
            }
            return null;
        }

        [BlockAction]
        public List<DateOption> GetDateOptionsAsync()
        {
            var dayOfWeeks = GetAttributeValues( AttributeKey.SendDaysOfTheWeek )
                .Select( dow => ( DayOfWeek ) Enum.Parse( typeof( DayOfWeek ), dow ) ).ToList();

            var previousWeeks = GetAttributeValue( AttributeKey.PreviousWeeksToShow ).AsIntegerOrNull() ?? 6;
            var futureWeeks = GetAttributeValue( AttributeKey.FutureWeeksToShow ).AsIntegerOrNull() ?? 1;

            var dateOptions = new List<DateOption>();
            var startDate = RockDateTime.Today.AddDays( -( previousWeeks * 7 ) );
            var endDate = RockDateTime.Today.AddDays( futureWeeks * 7 );

            for ( var dt = startDate; dt <= endDate; dt = dt.AddDays( 1 ) )
            {
                if ( dayOfWeeks.Contains( dt.DayOfWeek ) )
                {
                    dateOptions.Add( new DateOption
                    {
                        Text = dt.ToString( "MMMM d, yyyy" ),
                        Value = dt.ToString( "yyyyMMdd" )
                    } );
                }
            }

            return dateOptions;
        }

        [BlockAction]
        public PersonViewModel GetTargetPersonAsync( string personAliasGuid )
        {
            var rockContext = new RockContext();
            Guid guid;
            if ( Guid.TryParse( personAliasGuid, out guid ) )
            {
                var personAlias = new PersonAliasService( rockContext ).Get( guid );
                if ( personAlias == null )
                {
                    throw new Exception( "Invalid person alias GUID or person alias not found." );
                }

                // Retrieve the associated Person
                var person = new PersonService( rockContext ).Queryable( "Person" )
                    .AsNoTracking()
                    .Where( p => p.Id == personAlias.PersonId )
                    .Select( p => new PersonViewModel
                    {
                        Id = p.Id,
                        // Manually constructing the full name from first and last names
                        FullName = p.NickName + " " + p.LastName,
                        Email = p.Email,
                    } )
                    .FirstOrDefault();

                if ( person != null )
                {
                    return person;
                }
            }

            throw new Exception( "Invalid person GUID or person not found." );
        }

        [BlockAction]
        public CommunicationResult SendTestEmailAsync( string testEmail, int systemCommunicationId, int targetPersonId )
        {
            using ( var rockContext = new RockContext() )
            {
                var systemCommunicationService = new SystemCommunicationService( rockContext );
                var personService = new PersonService( rockContext );

                // Fetch the system communication
                var systemCommunication = systemCommunicationService.Get( systemCommunicationId );
                if ( systemCommunication == null )
                {
                    return new CommunicationResult { Success = false, ErrorMessage = "System Communication not found." };
                }

                // Fetch the target person
                var targetPerson = personService.Get( targetPersonId );
                if ( targetPerson == null )
                {
                    return new CommunicationResult { Success = false, ErrorMessage = "Target person not found." };
                }

                // Temporarily change the person's email address
                string originalEmail = targetPerson.Email;
                targetPerson.Email = testEmail;
                rockContext.SaveChanges();

                try
                {
                    // Prepare the email
                    var rockEmailMessage = new RockEmailMessage( systemCommunication.Guid );

                    // Append Lava Template if any
                    var lavaTemplateAppend = GetAttributeValue( AttributeKey.LavaTemplateAppend );
                    if ( !string.IsNullOrWhiteSpace( lavaTemplateAppend ) )
                    {
                        rockEmailMessage.Message = lavaTemplateAppend + rockEmailMessage.Message;
                    }

                    // Remove Lava Debug command
                    rockEmailMessage.Message = rockEmailMessage.Message.Replace( PageConstants.LavaDebugCommand, string.Empty );

                    // Prepare merge fields
                    var mergeFields = new Dictionary<string, object> { { MergeFieldKey.Person, targetPerson } };

                    // Attempt to get the send data from the URL params
                    string sendDateParam = RequestContext.GetPageParameter( "PublicationDate" );
                    DateTime sendDate;
                    if ( DateTime.TryParse( sendDateParam, out sendDate ) )
                    {
                        // Add the "SendDateTime" merge field if a valid date is provided
                        mergeFields.Add( MergeFieldKey.SendDateTime, sendDate.ToString( "MMMM d, yyyy" ) );
                    }

                    // Set the recipient with merge fields
                    rockEmailMessage.AddRecipient( new RockEmailMessageRecipient( targetPerson, mergeFields ) );
                    rockEmailMessage.CreateCommunicationRecord = false;

                    // Set the From Name and From Email based on System Communication or Global Attributes
                    SetEmailFromDetails( rockEmailMessage, systemCommunication );

                    // Prepare the subject, removing carriage returns and line feeds, as well as enforcing max length
                    rockEmailMessage.Subject = Regex.Replace( rockEmailMessage.Subject, @"\r\n?|\n", string.Empty ).Left( 998 );

                    // Send the email
                    var errors = new List<string>();
                    try
                    {
                        rockEmailMessage.Send( out errors );
                    }
                    catch (Exception ex )
                    {
                        errors.Add( ex.Message );
                    }
                    if ( errors.Any() )
                    {
                        return new CommunicationResult { Success = false, ErrorMessage = string.Join( ", ", errors ) };
                    }
                }
                finally
                {
                    // Revert the target person's email to it's original value
                    targetPerson.Email = originalEmail;
                    rockContext.SaveChanges();
                }

                return new CommunicationResult { Success = true };
            }
        }

        #endregion

        #region Helper Classes

        public class PersonViewModel
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
        }

        public class CommunicationResult
        {
            public bool Success { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class DateOption
        {
            public string Text { get; set; }
            public string Value { get; set; }
        }

        #endregion
    }
}
