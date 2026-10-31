#if UNITY_EDITOR
using System.Collections;
using DarkLord.Actors;
using DarkLord.Dungeon;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    [Category("FacilityDepth")]
    public sealed class FacilityDepthTests
    {
        private GameObject facility, actor;
        [TearDown] public void Cleanup()
        {
            if(actor!=null) Object.DestroyImmediate(actor);
            if(facility!=null) Object.DestroyImmediate(facility);
        }
        private void Spawn()
        {
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/MinionRecruitment.prefab");
            facility=Object.Instantiate(prefab,new Vector3(1000,1000),Quaternion.identity);
            actor=new GameObject("Facility collision actor");actor.transform.position=new Vector3(998,1000.5625f);
            actor.AddComponent<CircleCollider2D>().radius=.24f;
            actor.AddComponent<ActorHealth>().Configure(Faction.Dungeon,100);
            actor.AddComponent<TopDownMotor>();
        }
        [UnityTest] public IEnumerator SolidFootprintStopsWalkingAndRuinsAllowPassage()
        {
            Spawn();var motor=actor.GetComponent<TopDownMotor>();motor.Move(Vector2.right);
            yield return new WaitForSeconds(.65f);
            Assert.That(actor.transform.position.x,Is.LessThan(999.2f));
            Assert.That(facility.transform.position.x,Is.EqualTo(1000).Within(.001f));
            facility.GetComponent<ActorHealth>().Kill();
            yield return new WaitForSeconds(.8f);
            Assert.That(actor.transform.position.x,Is.GreaterThan(1000.5f));
        }
        [UnityTest] public IEnumerator BehindIsOccludedFrontIsVisibleAndFloorLightStaysBelow()
        {
            Spawn();var body=facility.transform.Find("Intact").GetComponent<SpriteRenderer>();
            var light=facility.transform.Find("Intact/Crimson floor light").GetComponent<SpriteRenderer>();
            int lightOrder=light.sortingOrder;
            actor.transform.position=new Vector3(1000,1001.2f);yield return null;yield return null;
            Assert.That(actor.GetComponent<SortingGroup>().sortingOrder,Is.LessThan(body.sortingOrder));
            actor.transform.position=new Vector3(1000,999.8f);yield return null;yield return null;
            Assert.That(actor.GetComponent<SortingGroup>().sortingOrder,Is.GreaterThan(body.sortingOrder));
            facility.transform.position+=Vector3.down*2;yield return null;yield return null;
            Assert.That(actor.GetComponent<SortingGroup>().sortingOrder,Is.LessThan(body.sortingOrder));
            Assert.AreEqual(lightOrder,light.sortingOrder);
        }
        [UnityTest] public IEnumerator OnlyOverlappingLivingPlayerBehindFadesAndRestores()
        {
            Spawn();var depth=facility.GetComponent<FacilityDepthSorting>();
            depth.SetOcclusionTarget(actor.GetComponent<ActorHealth>());
            var body=facility.transform.Find("Intact").GetComponent<SpriteRenderer>();
            var light=facility.transform.Find("Intact/Crimson floor light").GetComponent<SpriteRenderer>();
            float lightAlpha=light.color.a;
            actor.GetComponent<Rigidbody2D>().position=new Vector2(1000,1001.1f);
            yield return new WaitForSeconds(.2f);
            Assert.That(body.color.a,Is.EqualTo(.45f).Within(.01f));Assert.AreEqual(lightAlpha,light.color.a);
            actor.GetComponent<Rigidbody2D>().position=new Vector2(1002,1001.1f);
            yield return new WaitForSeconds(.2f);Assert.AreEqual(1,body.color.a);
            actor.GetComponent<Rigidbody2D>().position=new Vector2(1000,1000);
            yield return new WaitForSeconds(.2f);Assert.AreEqual(1,body.color.a);
            actor.GetComponent<Rigidbody2D>().position=new Vector2(1000,1001.1f);
            yield return new WaitForSeconds(.2f);Assert.That(body.color.a,Is.LessThan(.5f));
            depth.enabled=false;Assert.AreEqual(1,body.color.a);
        }
    }
}
#endif
