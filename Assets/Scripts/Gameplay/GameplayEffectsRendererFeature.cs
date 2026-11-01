using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace DarkLord.Gameplay
{
    // Draw only tagged VFX after the dungeon grade. An actor alpha mask preserves silhouettes.
    public sealed class GameplayEffectsRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Material actorMaskMaterial;
        private EffectsPass pass;

        public override void Create() => pass = new EffectsPass
        { renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing };

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (actorMaskMaterial == null || renderingData.cameraData.cameraType == CameraType.Preview) return;
            pass.maskMaterial = actorMaskMaterial;
            renderer.EnqueuePass(pass);
        }

        private sealed class EffectsPass : ScriptableRenderPass
        {
            internal Material maskMaterial;
            private static readonly ShaderTagId EffectTag = new ShaderTagId("DarkLordEffects");
            private static readonly int MaskId = Shader.PropertyToID("_GameplayActorMask");
            private sealed class PassData
            {
                public RendererListHandle renderers;
                public Material maskMaterial;
                public int cullingMask;
                public System.Collections.Generic.List<SpriteRenderer> bodies;
            }

            public override void RecordRenderGraph(RenderGraph graph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var rendering = frameData.Get<UniversalRenderingData>();
                var camera = frameData.Get<UniversalCameraData>();
                var light = frameData.Get<UniversalLightData>();
                var descriptor = camera.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.depthStencilFormat = GraphicsFormat.None;
                descriptor.msaaSamples = 1;
                descriptor.graphicsFormat = GraphicsFormat.R8_UNorm;
                var mask = UniversalRenderer.CreateRenderGraphTexture(graph, descriptor, "Gameplay actor silhouettes", true);
                using (var builder = graph.AddRasterRenderPass<PassData>("Gameplay effects: actor silhouettes", out var data))
                {
                    data.maskMaterial = maskMaterial;
                    data.cullingMask = camera.camera.cullingMask;
                    data.bodies = DarkLord.Actors.ActorDepthSorting.EffectBodies;
                    builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                    builder.SetGlobalTextureAfterPass(mask, MaskId);
                    builder.SetRenderFunc(static (PassData d, RasterGraphContext ctx) =>
                    {
                        foreach (var body in d.bodies)
                            if (body != null && body.enabled && body.gameObject.activeInHierarchy &&
                                (d.cullingMask & (1 << body.gameObject.layer)) != 0)
                                ctx.cmd.DrawRenderer(body, d.maskMaterial, 0, 0);
                    });
                }
                var effectDrawing = RenderingUtils.CreateDrawingSettings(EffectTag, rendering, camera, light, SortingCriteria.CommonTransparent);
                var effectFilter = new FilteringSettings(RenderQueueRange.transparent);
                using (var builder = graph.AddRasterRenderPass<PassData>("Gameplay effects: original color", out var data))
                {
                    data.renderers = graph.CreateRendererList(new RendererListParams(rendering.cullResults, effectDrawing, effectFilter));
                    builder.UseRendererList(data.renderers);
                    builder.UseGlobalTexture(MaskId, AccessFlags.Read);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.SetRenderFunc(static (PassData d, RasterGraphContext ctx) => ctx.cmd.DrawRendererList(d.renderers));
                }
            }
        }
    }
}
