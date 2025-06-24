using System.Numerics;
using RedLight.Scene;
using RedLight.Utils;
using Serilog;
using Silk.NET.OpenGL;
using StbImageSharp;

namespace RedLight.Graphics;

public struct CubeMapFace
{
    public string ResourceName;
    public CubeMapSide Side;
}

public class CubeMap
{
    public uint TextureID { get; private set; }
    public uint VAO { get; private set; }
    public uint VBO { get; private set; }
    private RLGraphics _graphics;

    // Skybox cube vertices (positions only)
    private static readonly float[] SkyboxVertices = {
        // positions          
        -1.0f,  1.0f, -1.0f,
        -1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,

        -1.0f, -1.0f,  1.0f,
        -1.0f, -1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,

         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,

        -1.0f, -1.0f,  1.0f,
        -1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f, -1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,

        -1.0f,  1.0f, -1.0f,
         1.0f,  1.0f, -1.0f,
         1.0f,  1.0f,  1.0f,
         1.0f,  1.0f,  1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f,  1.0f, -1.0f,

        -1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f, -1.0f,
         1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,
         1.0f, -1.0f,  1.0f
    };
    
    public CubeMap(RLGraphics graphics, List<CubeMapFace> faces)
    {
        _graphics = graphics;
        var gl = graphics.OpenGL;
        
        LoadTexture(gl, faces);
        SetupMesh(gl);
        AddShader();
        
        Log.Debug("CubeMap created with texture ID: {TextureID}", TextureID);
    }

    private void AddShader()
    {
        ShaderManager.Instance.TryAdd("skybox",
            new RLShaderBundle(_graphics,
                RLFiles.GetResourceAsString("RedLight.Resources.Shaders.cubemap.vert"),
                RLFiles.GetResourceAsString("RedLight.Resources.Shaders.cubemap.frag")));
    }

    private unsafe void SetupMesh(GL gl)
    {
        VAO = gl.GenVertexArray();
        VBO = gl.GenBuffer();

        gl.BindVertexArray(VAO);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, VBO);

        unsafe
        {
            fixed (float* vertices = SkyboxVertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(SkyboxVertices.Length * sizeof(float)), 
                    vertices, BufferUsageARB.StaticDraw);
            }
        }

        // Position attribute
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(0);

        gl.BindVertexArray(0);
    }

    public void Render(Camera camera)
    {
        var gl = _graphics.OpenGL;
        var shader = ShaderManager.Instance.Get("skybox");

        // Disable depth writing
        gl.DepthFunc(DepthFunction.Lequal);
        
        gl.UseProgram(shader.Program.ProgramHandle);

        // Remove translation from view matrix (keep only rotation)
        var view = camera.View;
        var viewNoTranslation = new Matrix4x4(
            view.M11, view.M12, view.M13, 0,
            view.M21, view.M22, view.M23, 0,
            view.M31, view.M32, view.M33, 0,
            0, 0, 0, 1
        );

        // Set uniforms
        shader.SetUniform("view", viewNoTranslation);
        shader.SetUniform("projection", camera.Projection);
        shader.SetUniform("skybox", 0);

        // Bind and render
        gl.BindVertexArray(VAO);
        gl.ActiveTexture(TextureUnit.Texture0);
        gl.BindTexture(TextureTarget.TextureCubeMap, TextureID);
        gl.DrawArrays(PrimitiveType.Triangles, 0, 36);
        
        gl.BindVertexArray(0);
        
        // Re-enable depth writing
        gl.DepthFunc(DepthFunction.Less);
    }

    public void Bind(TextureUnit unit = TextureUnit.Texture0)
    {
        var gl = _graphics.OpenGL;
        gl.ActiveTexture(unit);
        gl.BindTexture(TextureTarget.TextureCubeMap, TextureID);
    }
    
    public void Delete()
    {
        var gl = _graphics.OpenGL;
        gl.DeleteTexture(TextureID);
        gl.DeleteVertexArray(VAO);
        gl.DeleteBuffer(VBO);
    }

    private void LoadTexture(GL gl, List<CubeMapFace> faces)
    {
        TextureID = gl.GenTexture();
        gl.BindTexture(TextureTarget.TextureCubeMap, TextureID);

        foreach (var face in faces)
        {
            try
            {
                var directory = RLFiles.GetResourcePath(face.ResourceName);
                ImageResult imageResult;
                
                using var fileStream = File.OpenRead(directory);
                imageResult = ImageResult.FromStream(fileStream, ColorComponents.RedGreenBlueAlpha);
                
                unsafe
                {
                    fixed (byte* ptr = imageResult.Data)
                    {
                        var target = (TextureTarget)((int)TextureTarget.TextureCubeMapPositiveX + (int)face.Side - 1);
                        gl.TexImage2D(target, 0, InternalFormat.Rgba,
                            (uint)imageResult.Width, (uint)imageResult.Height, 0,
                            PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
                    }
                }
                
                Log.Debug("Loaded cubemap face: {Side} from {Resource}", face.Side, face.ResourceName);
            }
            catch (Exception ex)
            {
                Log.Error("Failed to load cubemap face {Side}: {Error}", face.Side, ex.Message);
            }
        }
        
        // Set texture parameters
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)GLEnum.ClampToEdge);
        
        gl.BindTexture(TextureTarget.TextureCubeMap, 0);
    }
    
    public static CubeMap CreateDefault(RLGraphics graphics)
    {
        var faces = new List<CubeMapFace>
        {
            new() { ResourceName = "RedLight.Resources.Textures.CubeMaps.right.png", Side = CubeMapSide.Right },
            new() { ResourceName = "RedLight.Resources.Textures.CubeMaps.left.png", Side = CubeMapSide.Left },
            new() { ResourceName = "RedLight.Resources.Textures.CubeMaps.up.png", Side = CubeMapSide.Top },
            new() { ResourceName = "RedLight.Resources.Textures.CubeMaps.down.png", Side = CubeMapSide.Bottom },
            new() { ResourceName = "RedLight.Resources.Textures.CubeMaps.front.png", Side = CubeMapSide.Front },
            new() { ResourceName = "RedLight.Resources.Textures.CubeMaps.back.png", Side = CubeMapSide.Back }
        };
        
        return new CubeMap(graphics, faces);
    }
    
    public static CubeMap ConvertImageToSkyboxTexture(RLGraphics graphics, string imagePath, int cubemapSize = 512, bool useKubi = true)
    {
        if (useKubi && File.Exists(imagePath))
        {
            // First check if Python is initialized
            if (!PythonSetup.IsInitialized)
            {
                Log.Error("Python environment not initialized. Call PythonSetup.Initialize() first.");
                throw new InvalidOperationException("Python environment not initialized");
            }

            if (!Kubi.IsKubiAvailable())
            {
                Log.Error("Kubi dependencies not available");
                throw new InvalidOperationException("Required Python packages (pyvips, numpy) not available for cubemap conversion");
            }

            return ConvertWithKubi(graphics, imagePath, cubemapSize);
        }
        else
        {
            Log.Error("Error with converting image to skybox texture - file not found or Kubi disabled");
            throw new InvalidOperationException($"Cannot convert image to cubemap: file not found ({imagePath}) or Kubi disabled");
        }
    }

    public static CubeMap ConvertWithKubi(RLGraphics graphics, string imagePath, int cubemapSize = 512)
    {
        try
        {
            // Create temporary directory for cubemap faces
            string tempDir = Path.Combine(Path.GetTempPath(), "RedLight_Cubemap", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            string baseName = Path.GetFileNameWithoutExtension(imagePath);
            string outputPath = Path.Combine(tempDir, baseName);
            
            var options = new Kubi.CubemapOptions
            {
                InputPath = imagePath,
                OutputPath = outputPath,
                Size = cubemapSize,
                Transform = Kubi.Transform.EAC, // Use equi-angular cubemap for better quality
                Layout = Kubi.Layout.Separate, // Generate individual face files
                Resample = Kubi.Resample.Bicubic, // Higher quality resampling
                FaceNames = new[] { "right", "left", "top", "bottom", "front", "back" }
            };
            
            // Generate cubemap faces
            bool success = Kubi.GenerateCubemap(options);
            
            if (!success)
            {
                throw new ApplicationException("Kubi conversion failed :(");
            }
            
            // Create CubeMap from generated faces
            var faces = new List<CubeMapFace>
            {
                new() { ResourceName = Path.Combine(tempDir, $"{baseName}_right.png"), Side = CubeMapSide.Right },
                new() { ResourceName = Path.Combine(tempDir, $"{baseName}_left.png"), Side = CubeMapSide.Left },
                new() { ResourceName = Path.Combine(tempDir, $"{baseName}_top.png"), Side = CubeMapSide.Top },
                new() { ResourceName = Path.Combine(tempDir, $"{baseName}_bottom.png"), Side = CubeMapSide.Bottom },
                new() { ResourceName = Path.Combine(tempDir, $"{baseName}_front.png"), Side = CubeMapSide.Front },
                new() { ResourceName = Path.Combine(tempDir, $"{baseName}_back.png"), Side = CubeMapSide.Back }
            };
            
            var cubeMap = new CubeMap(graphics, faces);
            
            // Cleanup temporary files
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to cleanup temporary directory: {Error}", ex.Message);
            }
            
            return cubeMap;
        }
        catch (Exception ex)
        {
            Log.Error("Error using Kubi for cubemap conversion: {Error}", ex.Message);
            throw new InvalidOperationException();
        }
    }
}

public enum CubeMapSide
{
    Right = 1,   // +X
    Left = 2,    // -X
    Top = 3,     // +Y
    Bottom = 4,  // -Y
    Front = 5,   // +Z
    Back = 6     // -Z
}