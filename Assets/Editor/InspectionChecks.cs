using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi.Editor
{
    public static class InspectionChecks
    {
        public static void Run()
        {
            var titles = new HashSet<string>();
            var evidence = new HashSet<string>();
            int[] positions = new int[3];
            foreach (string node in new[] { "shop", "records", "trace_desk" })
            {
                var nodePositions = new HashSet<int>();
                for (int i = 0; i < 3; i++)
                {
                    InspectionCase item = InspectionCases.Get(node, i);
                    Require(item != null, node + " case " + i + " is null");
                    foreach (string field in new[] { item.title, item.leftTitle, item.leftText, item.rightTitle,
                        item.rightText, item.question, item.success, item.failure })
                        Require(!string.IsNullOrWhiteSpace(field), node + " case " + i + " has an empty field");
                    Require(item.answers != null && item.answers.Length == 3, "Each case must have three answers");
                    Require(item.correct >= 0 && item.correct < 3, "Correct answer index is out of range");
                    var answers = new HashSet<string>();
                    foreach (string answer in item.answers)
                        Require(!string.IsNullOrWhiteSpace(answer) && answers.Add(answer), "Empty or duplicate answer");
                    Require(titles.Add(item.title), "Repeated case title: " + item.title);
                    Require(evidence.Add(item.leftText + "\n---\n" + item.rightText), "Repeated evidence pair");
                    Require(item.leftText != item.rightText, "Left and right evidence must differ");
                    positions[item.correct]++; nodePositions.Add(item.correct);
                    InspectionCase again = InspectionCases.Get(node, i);
                    Require(!object.ReferenceEquals(item, again) && !object.ReferenceEquals(item.answers, again.answers),
                        "Get must return independent case data");
                }
                Require(nodePositions.Count == 3, node + " should rotate the correct answer through all positions");
            }
            Require(titles.Count == 9 && evidence.Count == 9, "Expected nine distinct cases");
            Require(positions[0] == 3 && positions[1] == 3 && positions[2] == 3, "Answer positions must be balanced");
            bool invalidIndex = false, invalidNode = false;
            try { InspectionCases.Get("shop", 3); } catch (ArgumentOutOfRangeException) { invalidIndex = true; }
            try { InspectionCases.Get("home", 0); } catch (ArgumentException) { invalidNode = true; }
            Require(invalidIndex && invalidNode, "Invalid inputs should fail clearly");
            Debug.Log("QIAOPI INSPECTION CHECKS PASSED: 9 distinct complete cases, 27 answer options, correct positions 3/3/3, fresh data on every call.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("QIAOPI INSPECTION CHECK FAILED: " + message);
        }
    }
}
