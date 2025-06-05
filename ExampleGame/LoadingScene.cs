using RedLight.Graphics;
using RedLight.Scene;
using RedLight.Utils;
using Silk.NET.Maths;

namespace ExampleGame;

public class LoadingScene : RLLoadingScene
{
    public LoadingScene(string targetSceneId) : base(targetSceneId)
    {
    }

    public override void RenderContent()
    {
        List<Vertex> vertices = new List<Vertex>();
        List<uint> indices = new List<uint>();

        vertices.Add(new Vertex
        {
            Position = new Vector3D<float>(-1.0f, -1.0f, 0.0f),
            TexCoords = new Vector2D<float>(1.0f, 0.0f),
            Normal = new Vector3D<float>(0.0f, 0.0f, 1.0f)
        });
        vertices.Add(new Vertex
        {
            Position = new Vector3D<float>(1.0f, -1.0f, 0.0f),
            TexCoords = new Vector2D<float>(0.0f, 0.0f),
            Normal = new Vector3D<float>(0.0f, 0.0f, 1.0f)
        });
        vertices.Add(new Vertex
        {
            Position = new Vector3D<float>(-1.0f, 1.0f, 0.0f),
            TexCoords = new Vector2D<float>(1.0f, 1.0f),
            Normal = new Vector3D<float>(0.0f, 0.0f, 1.0f)
        });
        vertices.Add(new Vertex
        {
            Position = new Vector3D<float>(1.0f, 1.0f, 0.0f),
            TexCoords = new Vector2D<float>(0.0f, 1.0f),
            Normal = new Vector3D<float>(0.0f, 0.0f, 1.0f)
        });

        indices.Add(0); indices.Add(2); indices.Add(1);
        indices.Add(1); indices.Add(2); indices.Add(3);

        Mesh quadMesh = new Mesh(Graphics, vertices, indices.ToArray());

        if (ShaderManager.TryGet("basic").vertexShader == null)
        {
            ShaderManager.TryAdd("basic",
                new RLShader(Graphics, ShaderType.Vertex, RLConstants.RL_BASIC_SHADER_VERT),
                new RLShader(Graphics, ShaderType.Fragment, RLConstants.RL_BASIC_SHADER_FRAG));
        }
        var shader = ShaderManager.Get("basic");
        quadMesh.AttachShader(shader.vertexShader, shader.fragmentShader);

        if (TextureManager.TryGet("loading_screen") == null)
        {
            TextureManager.Add("loading_screen",
                new RLTexture(Graphics, RLFiles.GetEmbeddedResourcePath("RedLight.Resources.Textures.loading_screen.png")));
        }

        quadMesh.AttachTexture(TextureManager.Get("loading_screen"));

        var transformableMesh = quadMesh.MakeTransformable();

        var camera = new Camera(Engine.Window.Window.Size);

        transformableMesh.Translate(new Vector3D<float>(0.0f, 0.0f, 0.0f));

        Graphics.Use(transformableMesh);
        Graphics.Update(camera, transformableMesh);
        transformableMesh.Target.Draw();
    }
}