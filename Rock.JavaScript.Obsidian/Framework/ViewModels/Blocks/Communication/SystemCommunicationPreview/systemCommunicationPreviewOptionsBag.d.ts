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

/** Class SystemCommunicationPreviewOptionsBag. */
export type SystemCommunicationPreviewOptionsBag = {
    /** Gets or sets the system communication. */
    systemCommunication: string | null;
    /** Gets or sets days of the week for send date. */
    sendDaysOfTheWeek: string | null;
    /** Gets or sets how many weeks prior to today's date to display in the list. */
    previousWeeksToShow: string | null;
    /** Gets or sets how many weeks after today's date to display in the list. */
    futureWeeksToShow: string | null;
    /** Gets or sets the enabled Lava commands. */
    enabledLavaCommands: string | null;
    /** Gets or sets the Lava template to append to the email message. */
    lavaTemplateAppend: string | null;
};
