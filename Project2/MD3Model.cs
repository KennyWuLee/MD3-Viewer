using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;

using System.IO;
using Paloma;

namespace Project2
{
    using SharpDX.Toolkit;
    using SharpDX.Toolkit.Graphics;
    using SharpDX.Toolkit.Input;

    struct MD3Header
    {
        public string ID;       	//  4 bytes "IDP3"
        public int version;		//  15
        public string file;		//  64 bytes
        public int flags;
        public int frameCount;
        public int tagCount;
        public int meshCount;
        public int skinCount;
        public int frameOffset;
        public int tagOffset;
        public int meshOffset;
        public int fileSize;
    };

    public struct Frame
    {
        public Vector3 minimums;
        public Vector3 maximums;
        public Vector3 position;
        public float scale;
        public string creator;		//  16 bytes
    };

    public struct Tag
    {
        public string name;		//  64 bytes
        public Vector3 position;
        public Matrix rotation;
    };

    public struct Skin
    {
        public string name;
        public int index;
    };

    public struct Vertex
    {
        public Vector3 vertex;
        public byte[] normal;
    };

    public struct MeshHeader
    {
        public string ID;    	//  4 bytes
        public string name;  	//  64 bytes
        public int flags;
        public int frameCount;
        public int skinCount;
        public int vertexCount;
        public int triangleCount;
        public int triangleOffset;
        public int skinOffset;
        public int textureVectorStart;
        public int vertexStart;
        public int meshSize;
    };

    public struct Mesh
    {
        public MeshHeader header;
        public Skin[] skins;
        public int[] triangleVertices;
        public Vector2[] textureCoordinates;
        public Vertex[] vertices;
        public int texture;
    };

    class MD3Model
    {
        MD3Header header;
        Frame[] frames; //the model's frames
        Tag[] tags; //the model's tags
        Mesh[] meshes; //the meshes for the model
        MD3Model[] links; //the model's links
        GraphicsDevice device;

        int startFrame;
        int endFrame;
        int nextFrame;
        float interpolation; //the value used to interpolate by
        int currentFrame;

        List<Texture2D> textures; 
        static Vector3[,] normals = new Vector3[256, 256];

        //constructor for the model
        public MD3Model(GraphicsDevice device)
        {
            this.device = device;
            interpolation = 0;
        }

        public void DrawAllModels(BasicEffect basicEffect, Matrix current, Matrix next)
        {
            this.DrawModel(basicEffect, current, next); //first, draw the current model
            for(int i = 0; i < links.Length; ++i) //then loop through the models linked to this model
                if(links[i] != null) //only draw models that aren't null
                {
                    Tag currentTag = tags[currentFrame * header.tagCount + i];
                    Matrix m = currentTag.rotation *  Matrix.Translation(currentTag.position);
                    Tag nextTag = tags[nextFrame * header.tagCount + i];
                    Matrix mNext = nextTag.rotation * Matrix.Translation(nextTag.position);

                    links[i].DrawAllModels(basicEffect,  m * current,  mNext * next); //draw the next model as transformed
                }
        }

        public void DrawModel(BasicEffect basicEffect, Matrix current, Matrix next)
        {
            for(int i = 0; i < meshes.Length; ++i) //loop through each mesh in the model
            {
                if (meshes[i].texture != -1) //if the mesh texture is initialized
                    basicEffect.Texture = textures[i]; //set the current texture
                int currentOffSet = currentFrame * meshes[i].header.vertexCount; 
                int nextOffSet = nextFrame * meshes[i].header.vertexCount; //find the current and the next offsets.
             

                VertexPositionNormalTexture[] vertices = new VertexPositionNormalTexture[meshes[i].header.triangleCount * 3]; //an array big enough to hold all the vertices
                for(int j = 0; j < meshes[i].triangleVertices.Length; ++j) //loop over all the triangle vertices in the mesh
                {
                    int index = meshes[i].triangleVertices[j] + currentOffSet;
                    int nextIndex = meshes[i].triangleVertices[j] + nextOffSet;
                    Vector3 position = meshes[i].vertices[index].vertex; //find the position
                    Vector3 nextPosition = meshes[i].vertices[nextIndex].vertex; //find the next position
                    //transform the positions by the next and current matrices
                    Vector3 transfPosition = (Vector3)Vector3.Transform(position, current);
                    Vector3 transfNextPosition = (Vector3)Vector3.Transform(nextPosition, next);
                    //transform the normals as well
                    Vector3 normal = normals[meshes[i].vertices[index].normal[0], meshes[i].vertices[index].normal[1]];
                    Vector3 nextNormal = normals[meshes[i].vertices[nextIndex].normal[0], meshes[i].vertices[nextIndex].normal[1]];
                    Vector3 transfNormal = Vector3.TransformNormal(normal, current);
                    Vector3 transfNextNormal = Vector3.TransformNormal(nextNormal, next);
                    Vector3 interpNormal;
                    Vector3 interpPosition;
                    interpPosition = Vector3.Lerp(transfPosition, transfNextPosition, interpolation); //interpolate between the positions
                    interpNormal = Vector3.Lerp(transfNormal, transfNextNormal, interpolation); //interpolate between the normals

                    Vector2 textureCoor = meshes[i].textureCoordinates[meshes[i].triangleVertices[j]]; //assign the texture coordinates

                    vertices[j] = new VertexPositionNormalTexture(interpPosition, interpNormal, textureCoor); 
                }
                //use the vertex buffer to draw
                Buffer<VertexPositionNormalTexture> vertexBuffer = Buffer.New<VertexPositionNormalTexture>(device, vertices.Length, BufferFlags.VertexBuffer); 
                vertexBuffer.SetData<VertexPositionNormalTexture>(vertices);
                VertexInputLayout layout = VertexInputLayout.New<VertexPositionNormalTexture>(0);


                device.SetVertexBuffer(vertexBuffer);
                device.SetVertexInputLayout(layout);

                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    device.Draw(PrimitiveType.TriangleList, vertices.Length);
                }
            }
        }

        public void UpdateFrame(float passedFrames)
        {
            interpolation += passedFrames; //increase interpolation value based on the number of frames that should have passed based on time
            if (interpolation > 1)
            {
                
                interpolation %= 1f; //if interpolation is greater than 1, make sure it is in the range of 0-1
                currentFrame = nextFrame; //since interpolation has gone over 1, we have to move to the next frame
                nextFrame++;
                if (nextFrame >= endFrame) //looping
                    nextFrame = startFrame;
            }
        }

        public void Link(string name, MD3Model model)
        {
            int i = 0;
            while (i < header.tagCount && !tags[i].name.StartsWith(name)) //the loop runs as long as we still should have tags and as long as the tag we are looking at does not start with the string we passed in
                ++i;
            if (i < header.tagCount)
                links[i] = model;
        }

        //method to load models
        public void LoadModel(string file)
        {
            BinaryReader reader = new BinaryReader(File.Open(file, FileMode.Open)); //open the file with binary reading because it is a binary file
            ReadHeader(reader);
            frames = new Frame[header.frameCount];
            tags = new Tag[header.frameCount * header.tagCount];
            meshes = new Mesh[header.meshCount];
            links = new MD3Model[header.tagCount];
            ReadFrames(reader);
            ReadTags(reader);
            ReadMeshes(reader);
            reader.Close();
        }

        //method to initialize an animation
        public void setAnimation(int startFrame, int totalFrames)
        {
            this.startFrame = startFrame;
            this.endFrame = startFrame + totalFrames;
            currentFrame = startFrame;
            nextFrame = startFrame + 1;
            //interpolation = 0;
        }

        //a method to load the textures
        public void LoadSkin(string file)
        {
            textures = new List<Texture2D>();
            StreamReader reader = new StreamReader(File.Open(file, FileMode.Open));
            string line;
            while((line = reader.ReadLine()) != null)
                if (!line.Equals("") && !line.StartsWith("tag_"))
                {
                    string[] split = line.Split(','); //the name and the file name are seperated by a comma
                    int i = 0;
                    while(i < meshes.Length && ! meshes[i].header.name.StartsWith(split[0]))
                        ++i;
                    if(i < meshes.Length)
                    {
                        Texture2D texture = LoadTexture(device, split[1]);
                        textures.Add(texture);
                        meshes[i].texture = textures.Count - 1;
                    }
                }
            reader.Close();
        }

        private void ReadMeshes(BinaryReader reader)
        {
            long currentPosition = header.meshOffset;
            Mesh mesh;
            for (int i = 0; i < meshes.Length; ++i)
            {
                reader.BaseStream.Seek(currentPosition, SeekOrigin.Begin);
                mesh.header = ReadMeshHeader(reader);
                //triangles
                reader.BaseStream.Seek(currentPosition + mesh.header.triangleOffset, SeekOrigin.Begin);
                mesh.triangleVertices = new int[mesh.header.triangleCount * 3];
                for (int j = 0; j < mesh.triangleVertices.Length; ++j)
                    mesh.triangleVertices[j] = reader.ReadInt32();
                //skins
                reader.BaseStream.Seek(currentPosition + mesh.header.skinOffset, SeekOrigin.Begin);
                mesh.skins = new Skin[mesh.header.skinCount];
                for (int j = 0; j < mesh.skins.Length; ++j)
                    mesh.skins[j] = ReadSkin(reader);
                //texture coordinates
                reader.BaseStream.Seek(currentPosition + mesh.header.textureVectorStart, SeekOrigin.Begin);
                mesh.textureCoordinates = new Vector2[mesh.header.vertexCount];
                for (int j = 0; j < mesh.textureCoordinates.Length; ++j)
                    mesh.textureCoordinates[j] = ReadVector2(reader);
                //verticies              
                reader.BaseStream.Seek(currentPosition + mesh.header.vertexStart, SeekOrigin.Begin);
                mesh.vertices = new Vertex[mesh.header.vertexCount * mesh.header.frameCount];
                for (int j = 0; j < mesh.vertices.Length; ++j)
                    mesh.vertices[j] = ReadVertex(reader);
                mesh.texture = -1;

                meshes[i] = mesh;

                currentPosition += mesh.header.meshSize;
            }
        }

        //a method to read an individual vertex
        private Vertex ReadVertex(BinaryReader reader)
        {
            Vertex vertex;
            vertex.vertex = (1.0f/64) * ReadVector3Short(reader); //note that model scaling occurs in the reading of the vertices
            vertex.normal = reader.ReadBytes(2);
            return vertex;
        }

        //read in a skin, composed of a name and an index
        private Skin ReadSkin(BinaryReader reader)
        {
            Skin skin;
            skin.name = BytesToString(reader.ReadBytes(64));
            skin.index = reader.ReadInt32();
            return skin;
        }

        //Reads in the mesh header
        private MeshHeader ReadMeshHeader(BinaryReader reader)
        {
            MeshHeader meshHeader;
            meshHeader.ID = BytesToString(reader.ReadBytes(4));
            meshHeader.name = BytesToString(reader.ReadBytes(64));
            meshHeader.flags = reader.ReadInt32();
            meshHeader.frameCount = reader.ReadInt32();
            meshHeader.skinCount = reader.ReadInt32();
            meshHeader.vertexCount = reader.ReadInt32();
            meshHeader.triangleCount = reader.ReadInt32();
            meshHeader.triangleOffset = reader.ReadInt32();
            meshHeader.skinOffset = reader.ReadInt32();
            meshHeader.textureVectorStart = reader.ReadInt32();
            meshHeader.vertexStart = reader.ReadInt32();
            meshHeader.meshSize = reader.ReadInt32();
            return meshHeader;
        }

        //testing method
        private void PrintMeshHeader(MeshHeader meshHeader)
        {
            Console.Out.WriteLine("ID: " + meshHeader.ID);
            Console.Out.WriteLine("name: " + meshHeader.name);
            Console.Out.WriteLine("flags: " + meshHeader.flags);
            Console.Out.WriteLine("frameCount: " + meshHeader.frameCount);
            Console.Out.WriteLine("skinCount: " + meshHeader.skinCount);
            Console.Out.WriteLine("vertexCount: " + meshHeader.vertexCount);
            Console.Out.WriteLine("triangleCount: " + meshHeader.triangleCount);
            Console.Out.WriteLine("triangleOffset: " + meshHeader.triangleOffset);
            Console.Out.WriteLine("skinOffset: " + meshHeader.skinOffset);
            Console.Out.WriteLine("textureVectorStart: " + meshHeader.textureVectorStart);
            Console.Out.WriteLine("vertexStart: " + meshHeader.vertexStart);
            Console.Out.WriteLine("meshSize: " + meshHeader.meshSize);
        }

        //read the tags
        private void ReadTags(BinaryReader reader)
        {
            reader.BaseStream.Seek(header.tagOffset, SeekOrigin.Begin); //seek to the beginning of the tags
            Tag tag;
            for (int i = 0; i < tags.Length; ++i)
            {
                tag.name = BytesToString(reader.ReadBytes(64));
                tag.position = ReadVector3(reader);
                tag.rotation = ReadMatrix(reader);
                tags[i] = tag;
            }
        }

        //read in the frame data
        private void ReadFrames(BinaryReader reader)
        {
            reader.BaseStream.Seek(header.frameOffset, SeekOrigin.Begin);
            Frame frame;
            for (int i = 0; i < frames.Length; ++i)
            {
                frame.minimums = ReadVector3(reader);
                frame.maximums = ReadVector3(reader);
                frame.position = ReadVector3(reader);
                frame.scale = reader.ReadSingle();
                frame.creator = BytesToString(reader.ReadBytes(16));
                frames[i] = frame;
            }
        }

        //read in the header
        private void ReadHeader(BinaryReader reader)
        {
            header.ID = BytesToString(reader.ReadBytes(4));
            header.version = reader.ReadInt32();
            header.file = BytesToString(reader.ReadBytes(64));
            header.flags = reader.ReadInt32();
            header.frameCount = reader.ReadInt32();
            header.tagCount = reader.ReadInt32();
            header.meshCount = reader.ReadInt32();
            header.skinCount = reader.ReadInt32();
            header.frameOffset = reader.ReadInt32();
            header.tagOffset = reader.ReadInt32();
            header.meshOffset = reader.ReadInt32();
            header.fileSize = reader.ReadInt32();
        }

        //testing method
        private void PrintHeader()
        {
            Console.Out.WriteLine("ID: " + header.ID);
            Console.Out.WriteLine("version: " + header.version);
            Console.Out.WriteLine("file: " + header.file);
            Console.Out.WriteLine("flags: " + header.flags);
            Console.Out.WriteLine("frameCount: " + header.frameCount);
            Console.Out.WriteLine("tagCount: " + header.tagCount);
            Console.Out.WriteLine("meshCount: " + header.meshCount);
            Console.Out.WriteLine("skinCount: " + header.skinCount);
            Console.Out.WriteLine("frameOffset: " + header.frameOffset);
            Console.Out.WriteLine("tagOffset: " + header.tagOffset);
            Console.Out.WriteLine("meshOffset: " + header.meshOffset);
            Console.Out.WriteLine("fileSize: " + header.fileSize);
        }

        //convers an array of bytes into a string
        private string BytesToString(byte[] data)
        {
            string s = "";
            for (int i = 0; i < data.Length; ++i)
                s += (char)data[i];
            return s;
        }

        //read data into a vector
        private Vector3 ReadVector3(BinaryReader reader)
        {
            float x, y, z;
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
            return new Vector3(x, y, z); //construct a vector from three discrete elements
        }

        //read in a vector with two elements
        private Vector2 ReadVector2(BinaryReader reader)
        {
            float x, y;
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            return new Vector2(x, y);
        }
        
        //Read the short vectors, composed of 16-bit integers
        private Vector3 ReadVector3Short(BinaryReader reader)
        {
            short x, y, z;
            x = reader.ReadInt16();
            y = reader.ReadInt16();
            z = reader.ReadInt16();
            return new Vector3(x, y, z);
        }

        //read in a matrix
        private Matrix ReadMatrix(BinaryReader reader)
        {
            float[] homogenized = new float[16]; //to represent a 4x4 matrix
            for (int i = 0; i < 9; ++i) //that is, loop through the top left 3x3 submatrix
                homogenized[i + i / 3] = reader.ReadSingle(); //assign each element in the submatrix by reading from the file
            homogenized[15] = 1; //set the bottom right corner to 1 for a homogenized matrix
            return new Matrix(homogenized); //return as a matrix
        }

        //Load textures
        public static Texture2D LoadTexture(GraphicsDevice device, string texturePath)
        {
            Texture2D texture;

            if (texturePath.ToLower().EndsWith(".tga"))
            {
                TargaImage image = new TargaImage(texturePath);
                texture = Texture2D.New(device, image.Header.Width, image.Header.Height, PixelFormat.R8G8B8A8.UNorm);
                Color[] data = new Color[image.Header.Height * image.Header.Width];
                for (int y = 0; y < image.Header.Height; y++)
                    for (int x = 0; x < image.Header.Width; x++)
                    {
                        System.Drawing.Color color = image.Image.GetPixel(x, y);
                        data[y * image.Header.Width + x] = new Color(color.R, color.G, color.B, color.A);
                    }
                image.Dispose();
                texture.SetData(data);
            }
            else
            {
                texture = Texture2D.Load(device, texturePath);
            }

            return texture;
        }

        public static void SetUp()
        {
            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    float alpha = (float)(2.0 * i * Math.PI / 255);
                    float beta = (float)(2.0 * j * Math.PI / 255);
                    normals[i, j].X = (float)(Math.Cos(beta) * Math.Sin(alpha));
                    normals[i, j].Y = (float)(Math.Sin(beta) * Math.Sin(alpha));
                    normals[i, j].Z = (float)(Math.Cos(alpha));
                }
            }
        }
    }
}