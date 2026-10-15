using System;
using System.Collections.Generic;
using UnityEngine;

namespace Qiaopi
{
    public partial class WorldGame
    {
        AmbientLife ambientLife;
        bool AmbientQA => Array.IndexOf(Environment.GetCommandLineArgs(), "-qiaopi-ambient-test") >= 0;

        void EnsureAmbientLife()
        {
            if (!world) return;
            ambientLife = world.GetComponentInChildren<AmbientLife>();
            if (!ambientLife) ambientLife = AmbientLife.Create(world.transform, loadedWorld, player.transform, MobileControls);
            ambientLife.Player = player.transform;
            if(InInquiryScene) {
                // Legacy uncustomized doubles stay suppressed. Residents now
                // have their own fixed faces, so sharing a base wardrobe/rig
                // with the counter clerk does not remove them from the street.
                var inquiryModels = new List<string> { "Xusheng", CharacterRoster.Get(CharacterRoster.ForNode(TaskNode)).model };
                AddAmbientStoryIdentity(inquiryModels, npc);
                AddAmbientStoryIdentity(inquiryModels, inquiryHelper);
                ambientLife.SetStoryModels(inquiryModels.ToArray());
            }
            else if(InTrade&&state.tradeShift.route=="courier")ambientLife.SetStoryModels(new[]{"Xusheng","MasterHe","Guide","Worker","Uncle"});
            else if(InTrade&&state.tradeShift.route=="shop")ambientLife.SetStoryModels(new[]{"Xusheng","Shopkeeper","Guide"});
            else if(WorkEvents.HasIncident(state)&&state.tradeShift.route=="dock")ambientLife.SetStoryModels(new[]{"Xusheng","Foreman","Clerk"});
            else if(InNanyang)ambientLife.SetStoryModels(new[]{"Xusheng",CharacterRoster.Get(NanyangTasks.NpcId(state)??CharacterRoster.ForNode(TaskNode)).model});
            else ambientLife.SetStoryModel(InFirstDay ? "Xusheng" : "");
            var reserved = new List<Vector3> {mission.npcPosition, sidePosition};
            if(firstDayCompanion)reserved.Add(firstDayCompanion.transform.position);
            if(nanyangNeighbour)reserved.Add(nanyangNeighbour.transform.position);
            if(InNanyang)foreach(var place in NanyangPlaces.ForWorld(loadedWorld))reserved.Add(place.position);
            if(InTrade)reserved.AddRange(TradePlaces.ForWorld(loadedWorld));
            if(workEventPartner)reserved.Add(workEventPartner.transform.position);
            if(workShopCustomer)reserved.Add(workShopCustomer.transform.position);
            foreach (string inquiryRoute in InquiryPlaces.Routes) if (InquiryPlaces.World(inquiryRoute) == loadedWorld) {
                for (int index = 0; index < 3; index++) reserved.Add(InquiryPlaces.Evidence(inquiryRoute, index));
                reserved.Add(InquiryPlaces.Return(inquiryRoute));
            }
            if(InInquiryScene && inquiryHelper)reserved.Add(inquiryHelper.transform.position);
            foreach (var relative in familyActors) if (relative) reserved.Add(relative.position);
            if (mission.activity == "inspect") {
                for (int i = 0; i < Mathf.Min(inspectionPositions.Length, mission.required); i++) reserved.Add(inspectionPositions[i]);
            } else if (mission.activity == "collect" || mission.activity == "deliver") {
                reserved.Add(mission.itemPosition); reserved.Add(mission.destination);
            }
            if (writingPlace != null) reserved.Add(writingPlace.approach);
            ambientLife.SetExclusions(reserved.ToArray());
        }

        static void AddAmbientStoryIdentity(List<string> models, GameObject actor)
        {
            if (!actor) return;
            var identity = actor.GetComponentInChildren<CharacterModelIdentity>();
            if (identity && !string.IsNullOrEmpty(identity.modelId) && !models.Contains(identity.modelId)) models.Add(identity.modelId);
        }

        void AmbientPurposeLabel()
        {
            if (!ambientLife || !ambientLife.Ready) return;
            var person = ambientLife.NearestPurpose(player.transform.position);
            if (person == null) return;
            Tag(person.Position + Vector3.up * (ModelLibrary.CharacterHeight(person.Schedule.model) + .18f),
                person.Schedule.title + " · " + person.Purpose, false);
        }
    }
}
