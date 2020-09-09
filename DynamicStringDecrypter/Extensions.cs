using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables.Rows;

namespace DynamicStringDecrypter
{
    public static class Extensions
    {
        /// <summary>
        /// Check to see if the current assembly contains the current supplied method.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static bool ExistsInAssembly(this MethodDefinition method, AssemblyDefinition assembly)
        {
            foreach (var module in assembly.Modules)
            {
                foreach (var type in module.GetAllTypes())
                {
                    //so it does not mistaken it as a system method
                    if (type.Methods.Contains(method)) return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Checks to see if the methods return type is a string and has greater than 0 parameters.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static bool HasValidSignature(this MethodDefinition method)
        {
            if (method.Signature.ReturnType.ElementType == ElementType.String && method.Parameters.Count != 0)
                return true;
            return false;
        }

    }
}