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

namespace Rock.ViewModels.Blocks.Communication.SystemCommunicationPreview
{
    /// <summary>
    /// The additional configuration options for the Nameless Person List block.
    /// </summary>
    public class SystemCommunicationPreviewOptionsBag
    {
        /// <summary>
        /// Get or set the system communication identifier.
        /// </summary>
        public string SystemCommunication { get; set; }

        /// <summary>
        /// Get or set the days of the week to send.
        /// </summary>
        public string SendDaysOfTheWeek { get; set; }

        /// <summary>
        /// Get or set the previous amount of weeks to show.
        /// </summary>
        public string PreviousWeeksToShow { get; set; }

        /// <summary>
        /// Get or set the future amount of weeks to show.
        /// </summary>
        public string FutureWeeksToShow { get; set; }

        /// <summary>
        /// Get or set the enabled lava commands.
        /// </summary>
        public string EnabledLavaCommands { get; set; }

        /// <summary>
        /// Get or set the lava template.
        /// </summary>
        public string LavaTemplateAppend { get; set; }
    }
}