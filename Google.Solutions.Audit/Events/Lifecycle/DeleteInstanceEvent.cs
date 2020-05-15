﻿//
// Copyright 2019 Google LLC
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//

using Google.Solutions.LogAnalysis.Logs;
using System.Diagnostics;

namespace Google.Solutions.LogAnalysis.Events.Lifecycle
{
    public class DeleteInstanceEvent : LifecycleEventBase, IInstanceStateChangeEvent
    {
        public const string Method = "v1.compute.instances.delete";

        protected override string SuccessMessage => "Instance deleted";
        protected override string ErrorMessage => "Deleting instance failed";

        internal DeleteInstanceEvent(LogRecord logRecord) : base(logRecord)
        {
            Debug.Assert(IsDeleteInstanceEvent(logRecord));
        }

        public static bool IsDeleteInstanceEvent(LogRecord record)
        {
            return record.IsActivityEvent &&
                record.ProtoPayload.MethodName == Method;
        }

        //---------------------------------------------------------------------
        // IInstanceStateChangeEvent.
        //---------------------------------------------------------------------

        public InstanceState ResultingState(InstanceState preState)
            => !IsError ? InstanceState.Deleted : preState;

        public bool IsValidInState(InstanceState preState)
            => preState != InstanceState.Deleted;
    }
}
