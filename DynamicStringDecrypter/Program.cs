using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Builder;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace DynamicStringDecrypter
{
    class Program
    {
        private static AssemblyDefinition _Assembly { get; set; }
        private static Assembly _AssemblyReflection { get; set; }
        private static string _AssemblyPath { get; set; }
        private static int _DecryptedCount { get; set; }
        private static int _FailedCount { get; set; }
        static void Main(string[] args)
        {
            //Support multiple decryption methods later (other value-types)
            //Maybe change the second phase to find the method by getting the user to give us the mdtoken
            Console.Title = "DynamicStringDecrypter - By N0P";
            LoadAssembly(args[0]);
            var decryptionMethod = GetDecryptionMethod();
            if (decryptionMethod != null)
            {
                Console.WriteLine($"Found Decryption Method[{decryptionMethod.Name}]");
                DecryptVariables(decryptionMethod);
            }
            else
            {
                Console.WriteLine("Failed to find decryption method! Please provide the method name:");
                decryptionMethod = GetDecryptionMethodByName(Console.ReadLine());
                if (decryptionMethod != null)
                {
                    Console.WriteLine($"Found Decryption Method[{decryptionMethod.Name}]");
                    DecryptVariables(decryptionMethod);
                }
                else
                {
                    Console.WriteLine("Sorry looks like we yet again failed. Closing app");
                }
            }
            Console.WriteLine($"[{_DecryptedCount}] Variables Sucessfully Decrypted");
            Console.WriteLine($"[{_FailedCount}] Variables Unsuccessfully Decrypted");
            
            var imageBuilder = new ManagedPEImageBuilder();
            var factory = new DotNetDirectoryFactory();
            factory.MetadataBuilderFlags = MetadataBuilderFlags.PreserveAll;
            factory.MethodBodySerializer = new CilMethodBodySerializer
            {
                ComputeMaxStackOnBuildOverride = false
            };
            imageBuilder.DotNetDirectoryFactory = factory;
            
            if(_AssemblyPath.Contains(".dll"))
                _Assembly.ManifestModule.Write(Path.GetFileNameWithoutExtension(_AssemblyPath) + "-deob.dll", imageBuilder);
            else
                _Assembly.ManifestModule.Write(Path.GetFileNameWithoutExtension(_AssemblyPath) + "-deob.exe", imageBuilder);
            Console.WriteLine("Done!");
            Console.ReadKey();
        }
        
        static void DecryptVariables(MethodDefinition decryptMethod)
        {
            foreach (var module in _Assembly.Modules)
            {
                foreach (var type in module.GetAllTypes())
                {
                    foreach (var method in type.Methods.Where(x => x.MethodBody != null))
                    {
                        for (var i = 0; i < method.CilMethodBody.Instructions.Count; i++)
                        {
                            if (method.CilMethodBody.Instructions[i].OpCode == CilOpCodes.Call)
                            {
                                if (method.CilMethodBody.Instructions[i].Operand is MethodDefinition definition &&
                                    definition == decryptMethod)
                                {
                                    var listOfArgs = new List<object>();
                                    var j = 1;
                                    if (j <= decryptMethod.Parameters.Count())
                                    {
                                        listOfArgs.Add(method.CilMethodBody.Instructions[i - j].Operand);
                                    }
                                    else
                                    {
                                        for(j = 1; j < decryptMethod.Parameters.Count(); j++)
                                            listOfArgs.Add(method.CilMethodBody.Instructions[i - j].Operand);
                                    }
                                    var resolvedMethod =
                                        _AssemblyReflection.ManifestModule.ResolveMethod(decryptMethod.MetadataToken
                                            .ToInt32());
                                    try
                                    {
                                        var result = resolvedMethod.Invoke(null, listOfArgs.ToArray());
                                        if (result != null)
                                        {
                                            method.CilMethodBody.Instructions[i].OpCode = CilOpCodes.Ldstr;
                                            method.CilMethodBody.Instructions[i].Operand = result;
                                            if (j <= decryptMethod.Parameters.Count())
                                            {
                                                method.CilMethodBody.Instructions[i - j].OpCode = CilOpCodes.Nop;
                                            }
                                            else
                                            {
                                                for(j = 1; j < decryptMethod.Parameters.Count(); j++)
                                                    method.CilMethodBody.Instructions[i - j].OpCode = CilOpCodes.Nop;
                                            }
                                            _DecryptedCount++;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        _FailedCount++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static MethodDefinition GetDecryptionMethod()
        {
            foreach (var module in _Assembly.Modules)
            {
                foreach (var type in module.GetAllTypes())
                {
                    foreach (var method in type.Methods.Where(x => x.MethodBody != null))
                    {
                        for (var i = 0; i < method.CilMethodBody.Instructions.Count; i++)
                        {
                            if (method.CilMethodBody.Instructions[i].OpCode == CilOpCodes.Call)
                            {
                                if (method.CilMethodBody.Instructions[i].Operand is MethodDefinition def &&
                                    def.ExistsInAssembly(_Assembly) && def.HasValidSignature())
                                {
                                    return def;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        static MethodDefinition GetDecryptionMethodByName(string methodName)
        {
            foreach (var module in _Assembly.Modules)
            {
                foreach (var type in module.GetAllTypes())
                {
                    foreach (var method in type.Methods.Where(x => x.MethodBody != null))
                    {
                        if (method.Name == methodName)
                        {
                            return method;
                        }
                    }
                }
            }

            return null;
        }
        
        static void LoadAssembly(string file)
        {
            try
            {
                _Assembly = AssemblyDefinition.FromFile(file);
                _AssemblyReflection = Assembly.LoadFrom(file);
                _AssemblyPath = file;
                Console.WriteLine($"Loaded[{_Assembly.Name}]");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}