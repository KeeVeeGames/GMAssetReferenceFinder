using System.Text;
using System.Text.RegularExpressions;

namespace GMAssetReferenceFinder
{
    internal class Program
    {
        private static CodeNode[] codeNodes;
        private static RoomNode[] roomNodes;
        private static SequenceNode[] sequenceNodes;
        private static ObjectNode[] objectNodes;
        private static ParticleNode[] particleNodes;
        private static SpriteNode[] spriteNodes;
        private static SoundNode[] soundNodes;
        private static ShaderNode[] shaderNodes;
        private static AssetNode[] searchableNodes;

        private static int unusedSequencesNumber;
        private static int unusedObjectsNumber;
        private static int unusedParticlesNumber;
        private static int unusedSpritesNumber;
        private static int unusedSoundsNumber;
        private static int unusedShadersNumber;

        private static string projectPath;

        static void Main(string[] args)
        {
            projectPath = args[0];

            Parallel.Invoke(
                () =>
                {
                    string[] codeFiles = Directory.GetFiles(projectPath, "*.gml", SearchOption.AllDirectories);
                    codeNodes = ReadAssets<CodeNode>(codeFiles);
                    Console.WriteLine($"Code files found: {codeNodes.Length}");
                },

                () =>
                {
                    string[] roomFiles = Directory.GetFiles($"{projectPath}\\rooms", "*.yy", SearchOption.AllDirectories);
                    roomNodes = ReadAssets<RoomNode>(roomFiles);
                    Console.WriteLine($"Room files found: {roomNodes.Length}");
                },

                () =>
                {
                    string[] sequenceFiles = Directory.GetFiles($"{projectPath}\\sequences", "*.yy", SearchOption.AllDirectories);
                    sequenceNodes = ReadAssets<SequenceNode>(sequenceFiles);
                    Console.WriteLine($"Sequence files found: {sequenceNodes.Length}");
                },

                () =>
                {
                    string[] objectFiles = Directory.GetFiles($"{projectPath}\\objects", "*.yy", SearchOption.AllDirectories);
                    objectNodes = ReadAssets<ObjectNode>(objectFiles);
                    Console.WriteLine($"Object files found: {objectNodes.Length}");
                },

                () =>
                {
                    string[] particleFiles = Directory.GetFiles($"{projectPath}\\particles", "*.yy", SearchOption.AllDirectories);
                    particleNodes = ReadAssets<ParticleNode>(particleFiles);
                    Console.WriteLine($"Particle files found: {particleNodes.Length}");
                },

                () =>
                {
                    string[] spriteFiles = Directory.GetFiles($"{projectPath}\\sprites", "*.yy", SearchOption.AllDirectories);
                    spriteNodes = ReadAssets<SpriteNode>(spriteFiles);
                    Console.WriteLine($"Sprite files found: {spriteNodes.Length}");
                },

                () =>
                {
                    string[] soundFiles = Directory.GetFiles($"{projectPath}\\sounds", "*.yy", SearchOption.AllDirectories);
                    soundNodes = ReadAssets<SoundNode>(soundFiles);
                    Console.WriteLine($"Sound files found: {soundNodes.Length}");
                },

                () =>
                {
                    string[] shaderFiles = Directory.GetFiles($"{projectPath}\\shaders", "*.yy", SearchOption.AllDirectories);
                    shaderNodes = ReadAssets<ShaderNode>(shaderFiles);
                    Console.WriteLine($"Shader files found: {shaderNodes.Length}");
                }
            );

            Console.WriteLine();

            searchableNodes = (AssetNode[])codeNodes.Concat((AssetNode[])roomNodes).Concat((AssetNode[])sequenceNodes).Concat((AssetNode[])objectNodes).Concat((AssetNode[])particleNodes).ToArray();
            
            Parallel.Invoke(
                () => FindReferences(sequenceNodes, searchableNodes),
                () => FindReferences(objectNodes, searchableNodes),
                () => FindReferences(particleNodes, searchableNodes),
                () => FindReferences(spriteNodes, searchableNodes),
                () => FindReferences(soundNodes, searchableNodes),
                () => FindReferences(shaderNodes, searchableNodes)
            );

            Parallel.Invoke(
                () => unusedSequencesNumber = ShowUnused("Unused Sequences:", sequenceNodes),
                () => unusedObjectsNumber = ShowUnused("Unused Objects:", objectNodes),
                () => unusedParticlesNumber = ShowUnused("Unused Particles:", particleNodes),
                () => unusedSpritesNumber = ShowUnused("Unused Sprites:", spriteNodes),
                () => unusedSoundsNumber = ShowUnused("Unused Sounds:", soundNodes),
                () => unusedShadersNumber = ShowUnused("Unused Shaders:", shaderNodes)
            );

            Console.WriteLine($"Unused Sequences number: {unusedSequencesNumber}");
            Console.WriteLine($"Unused Objects number: {unusedObjectsNumber}");
            Console.WriteLine($"Unused Particles number: {unusedParticlesNumber}");
            Console.WriteLine($"Unused Sprites number: {unusedSpritesNumber}");
            Console.WriteLine($"Unused Sounds number: {unusedSoundsNumber}");
            Console.WriteLine($"Unused Shaders number: {unusedShadersNumber}");
        }

        private static T[] ReadAssets<T>(string[] files) where T : AssetNode, new()
        {
            int filesLength = files.Length;
            T[] assets = new T[filesLength];

            for (int i = 0; i < filesLength; i++)
            {
                using (StreamReader streamReader = new StreamReader(files[i], Encoding.UTF8))
                {
                    T asset = new T();
                    asset.name = Path.GetFileNameWithoutExtension(files[i]);

                    if (((T) asset).needContent)
                    {
                        asset.content = streamReader.ReadToEnd();
                    }

                    assets[i] = asset;
                }
            }

            return assets;
        }

        private static void FindReferences(AssetNode[] assets, AssetNode[] searchables)
        {
            foreach (AssetNode asset in assets)
            {
                foreach (AssetNode searchable in searchables)
                {
                    if (asset.GetType() != searchable.GetType() || asset.name != searchable.name)
                    {
                        if (Regex.IsMatch(searchable.content, $"\\b{asset.name}\\b"))
                        {
                            searchable.next.Add(asset);
                            asset.prevoius.Add(searchable);
                            //Console.WriteLine($"{asset.name} found in {searchable.name}");
                        }
                    }
                }

            }
        }

        private static int ShowUnused(string message, AssetNode[] assets)
        {
            int number = 0;
            StringBuilder sb = new StringBuilder($"{message}\n");

            foreach (AssetNode asset in assets)
            {
                if (!asset.CheckReferenced())
                {
                    sb.AppendLine(asset.name);
                    number++;
                }
            }

            string list = sb.ToString();
            Console.WriteLine(list);
            File.WriteAllText(message.Replace(" ", String.Empty).Replace(":", ".txt"), list);

            return number;
        }


        abstract public class AssetNode
        {
            public string name;
            public string content;
            abstract public bool needContent { get; }
            public List<AssetNode> prevoius = new List<AssetNode>();
            public List<AssetNode> next = new List<AssetNode>();
            public  bool CheckReferenced()
            {
                bool referenced = true;

                if (prevoius.Count == 0)
                {
                    referenced = false;
                }
                else
                {
                    //foreach (AssetNode parentNode in prevoius)
                    //{
                    //    if (!parentNode.CheckReferenced())
                    //    {
                    //        referenced = false;
                    //        break;
                    //    }
                    //}
                }

                return referenced;
            }
        }

        public class CodeNode : AssetNode
        {
            public override bool needContent => true;
        }
        public class RoomNode : AssetNode
        {
            public override bool needContent => true;
        }
        public class SequenceNode : AssetNode
        {
            public override bool needContent => true;
        }
        public class ObjectNode : AssetNode
        {
            public override bool needContent => true;
        }
        public class ParticleNode : AssetNode
        {
            public override bool needContent => true;
        }

        public class SpriteNode : AssetNode
        {
            public override bool needContent => false;
        }
        public class SoundNode : AssetNode
        {
            public override bool needContent => false;
        }
        public class ShaderNode : AssetNode
        {
            public override bool needContent => false;
        }
    }
}
