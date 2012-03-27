// Copyright 2007-2010 The Apache Software Foundation.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace dropkick.Configuration.Dsl.Notes
{
    using System;

    public static class Extension
    {
        public static void Note(this ProtoServer protoServer, string note)
        {
            var proto = new NoteProtoTask(note);
            protoServer.RegisterProtoTask(proto);
        }

        /// <summary>
        /// Ads an 'Alert' to the output
        /// </summary>
        /// <param name="protoServer"></param>
        /// <param name="messageFormat"></param>
        /// <param name="args"></param>
        public static void StrongNote(this ProtoServer protoServer, string messageFormat, params object[] args) {
           var proto = new NoteProtoTask(DeploymentModel.DeploymentItemStatus.Alert, messageFormat, args);
           protoServer.RegisterProtoTask(proto);
        }
        public static void Note(this ProtoServer protoServer, string messageFormat, params object[] args) {
           var proto = new NoteProtoTask(messageFormat, args);
           protoServer.RegisterProtoTask(proto);
        }
        public static void Note(this ProtoServer protoServer, dropkick.DeploymentModel.DeploymentItemStatus status, string messageFormat, params object[] args) {
           var proto = new NoteProtoTask(status, messageFormat, args);
           protoServer.RegisterProtoTask(proto);
        }

        public static void Wait(this ProtoServer protoServer, TimeSpan span)
        {
            var proto = new WaitProtoTask(span);
            protoServer.RegisterProtoTask(proto);
        }
    }
}