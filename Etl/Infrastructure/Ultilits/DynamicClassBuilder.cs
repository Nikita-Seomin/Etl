namespace Etl.Infrastructure.Ultilits;

using System.Reflection;
using System.Reflection.Emit;

public static class DynamicClassBuilder
{
    public static Dictionary<int, Type> BuildClasses(Dictionary<int, List<int>> classAttrs)
    {
        var asmName = new AssemblyName("DynamicEtlClasses");
        var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
        var moduleBuilder = asmBuilder.DefineDynamicModule("MainModule");

        var result = new Dictionary<int, Type>();

        foreach (var pair in classAttrs)
        {
            int classId = pair.Key;
            var attrIds = pair.Value;

            string className = $"object_{classId}_";
            var tb = moduleBuilder.DefineType(className, TypeAttributes.Public | TypeAttributes.Class);

            foreach (var attrId in attrIds)
            {
                // 0 там, где target_field_id отсутствует, встречается только у корневого элемента xml, т.к. у него есть loader, но ссылка на верхний реестр отсутствует
                if (attrId != 0)
                {
                    string fieldName = $"attr_{attrId}_";
                    tb.DefineField(fieldName, typeof(string), FieldAttributes.Public);
                }
            }

            var t = tb.CreateType();
            result[classId] = t;
        }

        return result;
    }
}
