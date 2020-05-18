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

using Google.Solutions.Compute;
using Google.Solutions.LogAnalysis.History;
using NUnit.Framework;
using System;
using System.Linq;

namespace Google.Solutions.LogAnalysis.Test.History
{
    [TestFixture]
    public class TestNodeSetHistory
    {
        [Test]
        public void WhenInstancesAreFromFleet_ThenSetIsEmpty()
        {
            var instances = new[]
            {
                new InstanceHistory(
                    1,
                    new VmInstanceReference("project-1", "zone-1", "instance-1"),
                    InstanceHistoryState.MissingTenancy,
                    null,
                    Tenancy.Fleet,
                    null)
            };

            var nodes = NodeSetHistory.FromInstancyHistory(instances);
            Assert.IsFalse(nodes.Nodes.Any());
        }

        [Test]
        public void WhenInstanceHasNoPlacement_ThenSetIsEmpty()
        {
            var instances = new[]
            {
                new InstanceHistory(
                    1,
                    new VmInstanceReference("project-1", "zone-1", "instance-1"),
                    InstanceHistoryState.Complete,
                    new GlobalResourceReference("project-1", "images", "image-1"),
                    Tenancy.SoleTenant,
                    null)
            };

            var nodes = NodeSetHistory.FromInstancyHistory(instances);
            Assert.IsFalse(nodes.Nodes.Any());
        }

        [Test]
        public void WhenInstanceHasNonOverlappingPlacements_ThenPeakConcurrentPlacementsIsOne()
        {
            var instances = new[]
            {
                new InstanceHistory(
                    1,
                    new VmInstanceReference("project-1", "zone-1", "instance-1"),
                    InstanceHistoryState.MissingTenancy,
                    new GlobalResourceReference("project-1", "images", "image-1"),
                    Tenancy.SoleTenant,
                    new []
                    {
                        new InstancePlacement(
                            "server-1",
                            new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                            new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc)),
                        new InstancePlacement(
                            "server-1",
                            new DateTime(2019, 12, 3, 0, 0, 0, DateTimeKind.Utc),
                            new DateTime(2019, 12, 4, 0, 0, 0, DateTimeKind.Utc)),
                        new InstancePlacement(
                            "server-2",
                            new DateTime(2019, 12, 4, 0, 0, 0, DateTimeKind.Utc),
                            new DateTime(2019, 12, 5, 0, 0, 0, DateTimeKind.Utc)),
                    })
            };

            var nodes = NodeSetHistory.FromInstancyHistory(instances);
            Assert.AreEqual(2, nodes.Nodes.Count());

            var server1 = nodes.Nodes.First(n => n.ServerId == "server-1");
            Assert.AreEqual(1, server1.PeakConcurrentPlacements);

            var server2 = nodes.Nodes.First(n => n.ServerId == "server-2");
            Assert.AreEqual(1, server2.PeakConcurrentPlacements);
        }

        [Test]
        public void WhenInstanceHasSubsequentPlacements_ThenFirstAndLastUseAreExtremes()
        {
            var instances = new[]
            {
                new InstanceHistory(
                    1,
                    new VmInstanceReference("project-1", "zone-1", "instance-1"),
                    InstanceHistoryState.MissingTenancy,
                    new GlobalResourceReference("project-1", "images", "image-1"),
                    Tenancy.SoleTenant,
                    new []
                    {
                        new InstancePlacement(
                            "server-1",
                            new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                            new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc)),
                        new InstancePlacement(
                            "server-1",
                            new DateTime(2019, 12, 13, 0, 0, 0, DateTimeKind.Utc),
                            new DateTime(2019, 12, 14, 0, 0, 0, DateTimeKind.Utc)),
                        new InstancePlacement(
                            "server-2",
                            new DateTime(2019, 12, 4, 0, 0, 0, DateTimeKind.Utc),
                            new DateTime(2019, 12, 5, 0, 0, 0, DateTimeKind.Utc)),
                    })
            };

            var nodes = NodeSetHistory.FromInstancyHistory(instances);
            Assert.AreEqual(2, nodes.Nodes.Count());

            var server1 = nodes.Nodes.First(n => n.ServerId == "server-1");
            Assert.AreEqual(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc), server1.FirstUse);
            Assert.AreEqual(new DateTime(2019, 12, 14, 0, 0, 0, DateTimeKind.Utc), server1.LastUse);

            var server2 = nodes.Nodes.First(n => n.ServerId == "server-2");
            Assert.AreEqual(new DateTime(2019, 12, 4, 0, 0, 0, DateTimeKind.Utc), server2.FirstUse);
            Assert.AreEqual(new DateTime(2019, 12, 5, 0, 0, 0, DateTimeKind.Utc), server2.LastUse);
        }

        [Test]
        public void WhenInstanceHasOverlappingPlacements_ThenPeakConcurrentPlacementsIsTwo()
        {
            var instances = new[]
            {
                new InstanceHistory(
                    1,
                    new VmInstanceReference("project-1", "zone-1", "instance-1"),
                    InstanceHistoryState.MissingTenancy,
                    new GlobalResourceReference("project-1", "images", "image-1"),
                    Tenancy.SoleTenant,
                    new []
                    {
                        new InstancePlacement(
                            "server-1",
                            new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                            new DateTime(2019, 12, 4, 0, 0, 0, DateTimeKind.Utc))
                    }),
                new InstanceHistory(
                    2,
                    new VmInstanceReference("project-1", "zone-1", "instance-1"),
                    InstanceHistoryState.MissingTenancy,
                    new GlobalResourceReference("project-1", "images", "image-1"),
                    Tenancy.SoleTenant,
                    new []
                    {
                        new InstancePlacement(
                            "server-1",
                            new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                            new DateTime(2019, 12, 3, 0, 0, 0, DateTimeKind.Utc))
                    })
            };

            var nodes = NodeSetHistory.FromInstancyHistory(instances);
            Assert.AreEqual(1, nodes.Nodes.Count());

            var server1 = nodes.Nodes.First(n => n.ServerId == "server-1");
            Assert.AreEqual(2, server1.PeakConcurrentPlacements);
            Assert.AreEqual(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc), server1.FirstUse);
            Assert.AreEqual(new DateTime(2019, 12, 4, 0, 0, 0, DateTimeKind.Utc), server1.LastUse);
        }

        [Test]
        public void WhenInstanceHasOverlappingPlacements_ThenPlacementReturnsInstances()
        {
            var instances = new[]
            {
                new InstanceHistory(
                    1,
                    new VmInstanceReference("project-1", "zone-1", "instance-1"),
                    InstanceHistoryState.MissingTenancy,
                    new GlobalResourceReference("project-1", "images", "image-1"),
                    Tenancy.SoleTenant,
                    new []
                    {
                        new InstancePlacement(
                            "server-1",
                            new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                            new DateTime(2019, 12, 4, 0, 0, 0, DateTimeKind.Utc))
                    }),
                new InstanceHistory(
                    2,
                    new VmInstanceReference("project-1", "zone-1", "instance-1"),
                    InstanceHistoryState.MissingTenancy,
                    new GlobalResourceReference("project-1", "images", "image-1"),
                    Tenancy.SoleTenant,
                    new []
                    {
                        new InstancePlacement(
                            "server-1",
                            new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc),
                            new DateTime(2019, 12, 3, 0, 0, 0, DateTimeKind.Utc))
                    })
            };

            var nodes = NodeSetHistory.FromInstancyHistory(instances);
            Assert.AreEqual(1, nodes.Nodes.Count());

            var server1 = nodes.Nodes.First(n => n.ServerId == "server-1");
            Assert.AreEqual(2, server1.Placements.Count());

            var placement1 = server1.Placements.First(p => p.Instance.InstanceId == 1);
            Assert.AreEqual(new DateTime(2019, 12, 1, 0, 0, 0, DateTimeKind.Utc), placement1.From);
            Assert.AreEqual(new DateTime(2019, 12, 4, 0, 0, 0, DateTimeKind.Utc), placement1.To);

            var placement2 = server1.Placements.First(p => p.Instance.InstanceId == 2);
            Assert.AreEqual(new DateTime(2019, 12, 2, 0, 0, 0, DateTimeKind.Utc), placement2.From);
            Assert.AreEqual(new DateTime(2019, 12, 3, 0, 0, 0, DateTimeKind.Utc), placement2.To);
        }
    }
}
