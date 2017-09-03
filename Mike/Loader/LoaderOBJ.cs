using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OpenTK;

namespace Render.Loader
{
    public class LoaderOBJ
    {
        public readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        public readonly List<Mesh3D> Meshes = new List<Mesh3D>();

        private LoaderOBJ() { }

        public static LoaderOBJ Create(FileInfo fileInfo)
        {
            var obj = new LoaderOBJ();

            obj.Parse(fileInfo);

            return obj;
        }

        private void Parse(FileInfo fileInfo)
        {
            var vertIndex = new List<Vector3>();
            var texIndex = new List<Vector2>();
            var normIndex = new List<Vector3>();

            Mesh3D currentVGroup = null;

            using (var reader = new StreamReader(fileInfo.OpenRead()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var parameters = line.Trim().Split(' ');

                    float x;
                    float y;
                    float z;
                    switch (parameters[0])
                    {
                        case "mtllib": // material library
                            var path = Path.Combine(fileInfo.DirectoryName, parameters[1]);
                            ParseMTL(new FileInfo(path));
                            break;

                        case "v": // Vertex
                            x = float.Parse(parameters[1], CultureInfo.InvariantCulture.NumberFormat);
                            y = float.Parse(parameters[2], CultureInfo.InvariantCulture.NumberFormat);
                            z = float.Parse(parameters[3], CultureInfo.InvariantCulture.NumberFormat);
                            vertIndex.Add(new Vector3(x, y, z));
                            break;

                        case "vt": // TexCoord
                            x = float.Parse(parameters[1], CultureInfo.InvariantCulture.NumberFormat);
                            y = float.Parse(parameters[2], CultureInfo.InvariantCulture.NumberFormat);
                            texIndex.Add(new Vector2(x, 1.0f - y)); // NOTE: OpenGL texcoord Y axis is mirrored
                            break;

                        case "vn": // Normal
                            x = float.Parse(parameters[1], CultureInfo.InvariantCulture.NumberFormat);
                            y = float.Parse(parameters[2], CultureInfo.InvariantCulture.NumberFormat);
                            z = float.Parse(parameters[3], CultureInfo.InvariantCulture.NumberFormat);
                            normIndex.Add(new Vector3(x, y, z));
                            break;

                        case "g": // Polygon group name
                        case "o": // Polygon object name
                            if (currentVGroup != null)
                                Meshes.Add(currentVGroup);
                            currentVGroup = new Mesh3D {Name = parameters[1]};
                            break;

                        case "usemtl": // active Material
                            if (currentVGroup != null)
                                currentVGroup.Material = parameters[1];
                            break;

                        case "f": // Face
                            if (currentVGroup != null)
                            {
                                var face = ParseFace(parameters);
                                currentVGroup.Faces.Add(face);
                            }
                            break;
                    }
                }
            }

            if (currentVGroup != null)
                Meshes.Add(currentVGroup);

            foreach (var vertexGroup in Meshes)
            {
                BuildMesh(vertexGroup, vertIndex, texIndex, normIndex);
            }
        }

        private void ParseMTL(FileInfo fileInfo)
        {
            using (var reader = new StreamReader(fileInfo.OpenRead()))
            {
                Material currentMaterial = null;

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    float r, g, b;
                    var parameters = line.Trim().Split(' ');

                    switch (parameters[0])
                    {
                        case "newmtl":
                            var name = parameters[1];
                            currentMaterial = Materials[name] = new Material();
                            break;
                        case "Ka":
                            r = float.Parse(parameters[1]);
                            g = float.Parse(parameters[2]);
                            b = float.Parse(parameters[3]);
                            currentMaterial.ka[0] = r;
                            currentMaterial.ka[1] = g;
                            currentMaterial.ka[2] = b;
                            break;
                        case "Kd":
                            r = float.Parse(parameters[1]);
                            g = float.Parse(parameters[2]);
                            b = float.Parse(parameters[3]);
                            currentMaterial.kd[0] = r;
                            currentMaterial.kd[1] = g;
                            currentMaterial.kd[2] = b;
                            break;
                        case "Ks":
                            r = float.Parse(parameters[1]);
                            g = float.Parse(parameters[2]);
                            b = float.Parse(parameters[3]);
                            currentMaterial.ks[0] = r;
                            currentMaterial.ks[1] = g;
                            currentMaterial.ks[2] = b;
                            break;
                        case "Tr":
                        case "d":
                            currentMaterial.tr = float.Parse(parameters[1]);
                            break;
                        case "Ns":
                            currentMaterial.hardness = float.Parse(parameters[1]);
                            break;
                        case "illum":
                            currentMaterial.illum = int.Parse(parameters[1]);
                            break;
                        case "map_Kd": // main texure
                            currentMaterial.diffuseMap = Path.Combine(fileInfo.DirectoryName, parameters[1]);

                            //TODO: Load file.

                            break;
                        case "map_Ka":
                            currentMaterial.ambientMap = Path.Combine(fileInfo.DirectoryName, parameters[1]);
                            break;
                        case "map_Bump":
                            currentMaterial.normalMap = Path.Combine(fileInfo.DirectoryName, parameters[1]);
                            break;
                    }
                }
            }
        }

        private static Face ParseFace(string[] vertices)
        {
            var face = new Face();

            for (var i = 0; i < 3; i++)
            {
                var faceLine = vertices[i + 1];

                // Check if the UV is ommited and replace it with 0 if it is.
                faceLine = faceLine.Replace("//", "/0/");

                var vals = faceLine.Split('/');

                face.Pos[i] = uint.Parse(vals[0]);
                face.Uv[i] = uint.Parse(vals[1]);
                face.Norm[i] = uint.Parse(vals[2]);
            }

            return face;
        }

        private static void BuildMesh(Mesh3D mesh3D, List<Vector3> vertIndex, List<Vector2> texIndex, List<Vector3> normIndex)
        {
            var coordVerts = new List<Vector3>();
            var coordTex = new List<Vector2>();
            var coordNorm = new List<Vector3>();

            foreach (var face in mesh3D.Faces)
            {
                foreach (var u in face.Pos)
                {
                    coordVerts.Add(vertIndex[(int) u - 1]); // NOTE: OBJ indexes start at 1, not 0
                }

                foreach (var u in face.Uv)
                {
                    coordTex.Add(texIndex[(int) u - 1]); // NOTE: OBJ indexes start at 1, not 0
                }

                foreach (var u in face.Norm)
                {
                    coordNorm.Add(normIndex[(int) u - 1]); // NOTE: OBJ indexes start at 1, not 0
                }
            }
            mesh3D.CoordVerts = coordVerts;
            mesh3D.CoordTex = coordTex;
            mesh3D.CoordNorm = coordNorm;
        }

        // OBJ Helper struct for face vertex data indicies.
        public class Face
        {
            public readonly uint[] Norm = new uint[3]; // index
            public readonly uint[] Pos = new uint[3]; // index
            public readonly uint[] Uv = new uint[3]; // index
        }

        // OBJ Helper struct for storing vertex groups
        public class Mesh3D
        {
            public readonly List<Face> Faces = new List<Face>();
            public string Material;
            public string Name;

            public Mesh3D()
            {
                CoordVerts = new List<Vector3>();
                CoordTex = new List<Vector2>();
                CoordNorm = new List<Vector3>();
            }

            public List<Vector3> CoordVerts { get; internal set; }
            public List<Vector2> CoordTex { get; internal set; }
            public List<Vector3> CoordNorm { get; internal set; }

            public void Dimensions(out double width, out double length, out double height)
            {
                double maxx, minx, maxy, miny, maxz, minz;
                maxx = maxy = maxz = minx = miny = minz = 0;
                foreach (var vert in CoordVerts)
                {
                    if (vert.X > maxx) maxx = vert.X;
                    if (vert.Y > maxy) maxy = vert.Y;
                    if (vert.Z > maxz) maxz = vert.Z;
                    if (vert.X < minx) minx = vert.X;
                    if (vert.Y < miny) miny = vert.Y;
                    if (vert.Z < minz) minz = vert.Z;
                }
                width = maxx - minx;
                length = maxy - miny;
                height = maxz - minz;
            }
        }

        // Material Helper struct describing Material files
        public class Material
        {
            public string ambientMap;
            public string diffuseMap;
            public float hardness;
            public int illum;
            public Vector3 ka;
            public Vector3 kd;
            public Vector3 ks;

            public string normalMap;

            //public string specularMap;
            public float tr;
        }
    }
}
