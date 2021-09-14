using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Cav.Container
{
    /// <summary>
    /// Локатор-контейнер объектов. По умолчанию экземпляры кладет в кэш. То есть: объект - синглтон
    /// </summary>
    public static class Locator
    {
        private class PropSetDataT
        {
            public PropertyInfo Property { get; set; }
            public Object InstatnceObject { get; set; }
        }

        static Locator()
        {
            UseCache = true;
        }

        private static ConcurrentDictionary<Type, Object> cacheObjects = new ConcurrentDictionary<Type, object>();

        private static Object getObjectFromCache(Type type)
        {
            Object res = null;
            cacheObjects.TryGetValue(type, out res);
            return res;
        }

        private static void putObjectToCache(Object inst)
        {
            Type typrObj = inst.GetType();
            cacheObjects.TryAdd(typrObj, inst);
        }

        /// <summary>
        /// Дополнительные действия с объектом после создания
        /// </summary>
        [Obsolete("Будет удалено. Используйте интерфейс IInitInstance")]
        public static Action<Object> AdditionalSettingsObject { get; set; }

        /// <summary>
        /// Использовать кэш объектов (false - объект и зависимости создаются заново)
        /// </summary>
        [Obsolete("Будет удалено. Используйте атрибут AlwaysNewAttribute")]
        public static Boolean UseCache { get; set; }

        /// <summary>
        ///Получить экземпляр указанного типа 
        /// </summary>
        /// <param name="typeInstance"></param>
        /// <returns></returns>
        public static object GetInstance(Type typeInstance)
        {
            if (typeInstance == null)
                throw new ArgumentNullException(nameof(typeInstance));

            if ((Nullable.GetUnderlyingType(typeInstance) ?? typeInstance).IsValueType || typeInstance == typeof(string))
                return typeInstance.GetDefault();

            if (!typeInstance.IsClass)
                throw new ArgumentException($"{nameof(typeInstance)} {typeInstance.FullName} должен быть класс или интерфейс");

            if (typeInstance.IsAbstract)
                throw new ArgumentException($"{nameof(typeInstance)} {typeInstance.FullName}  не может бать абстрактным классом");

            var akaSingleton = typeInstance.GetCustomAttribute<AlwaysNewAttribute>() == null;

            object res = null;

            if ((UseCache || akaSingleton) && typeInstance.IsClass)
            {
                res = getObjectFromCache(typeInstance);
                if (res != null)
                    return res;
            }

            puhStackAndCheckRecursion(typeInstance);

            var constructor = typeInstance.GetConstructors().OrderBy(x => x.GetParameters().Length).FirstOrDefault();
            if (constructor == null)
                throw new ArgumentOutOfRangeException($"У типа {typeInstance.FullName} нет открытого конструктора");

            List<object> paramConstr = new List<object>();

            foreach (var constParam in constructor.GetParameters())
            {
                Object paramInstance = null;
                Type paramType = constParam.ParameterType;

                if (paramType.IsArray)
                {
                    Type typeInArray = paramType.GetElementType();
                    paramInstance = Convert.ChangeType(Locator.GetInstances(typeInArray), paramType);
                }
                else
                {
                    if (typeof(IEnumerable).IsAssignableFrom(paramType))
                        throw new ArgumentOutOfRangeException($"тип {typeInstance.FullName}. в конструкторе поддерживаются только массивы");
                }

                if (paramType.IsInterface)
                {
                    var instsesInterface = Locator.GetInstances(paramType);
                    if (instsesInterface.Length > 1)
                        throw new ArgumentException($"Интерфейс {paramType.FullName} имеет более одной реализации");
                    if (instsesInterface.Length == 0)
                        throw new ArgumentException($"Интерфейс {paramType.FullName} не имеет реализаций");
                    paramInstance = instsesInterface.GetValue(0);
                }

                if (paramInstance == null)
                    paramInstance = Locator.GetInstance(paramType);

                paramConstr.Add(paramInstance);
            }

            res = constructor.Invoke(paramConstr.ToArray());

            if (UseCache || akaSingleton)
                putObjectToCache(res);

            foreach (var propInfo in typeInstance.GetProperties())
            {
                if (propInfo.GetCustomAttribute<PropertyInjectAttribute>() == null)
                    continue;

                if (!propInfo.CanWrite)
                    throw new InvalidOperationException($"свойство {typeInstance.FullName}.{propInfo.Name} должно быть доступно для записи");

                propSetData.Value.Add(new PropSetDataT() { Property = propInfo, InstatnceObject = res });
            }

            popStack();

            AdditionalSettingsObject?.Invoke(res);

            (res as IInitInstance)?.InitInstance();

            return res;
        }


        private static ThreadLocal<Stack<String>> pathDependency = new ThreadLocal<Stack<string>>(() => new Stack<string>());
        private static ThreadLocal<List<PropSetDataT>> propSetData = new ThreadLocal<List<PropSetDataT>>(() => new List<PropSetDataT>());

        private static void popStack()
        {
            if (pathDependency.Value.Any())
                pathDependency.Value.Pop();

            if (!pathDependency.Value.Any())
                foreach (var prpData in propSetData.Value.ToArray())
                {
                    prpData.Property.SetValue(prpData.InstatnceObject, GetInstance(prpData.Property.PropertyType));
                    propSetData.Value.Remove(prpData);
                }
        }

        private static void puhStackAndCheckRecursion(Type typeInstance)
        {
            var type = pathDependency.Value.FirstOrDefault(x => x == typeInstance.FullName);
            if (type != null)
            {
                var promDep = new List<String>();
                string parDep = null;

                do
                {
                    parDep = pathDependency.Value.Pop();
                    promDep.Add(parDep);

                } while (parDep != type);


                String msg = $"{promDep.ToArray().Reverse().JoinValuesToString(" -> ")} -> {type}";
                throw new StackOverflowException("Обнаружена рекурсивная зависимость: " + msg);
            }

            pathDependency.Value.Push(typeInstance.FullName);
        }
        /// <summary>
        /// Получить экземпляры объектов типа - наследника указанного
        /// </summary>
        /// <param name="typeParent">Тип-родитель</param>
        /// <returns>Массив экземпляров</returns>
        public static Array GetInstances(Type typeParent)
        {
            List<Type> typeForCreate = new List<Type>();

            Func<Type, bool> predicat = null;

            if (typeParent.IsClass)
                predicat = (t) => t.IsSubclassOf(typeParent);

            if (typeParent.IsInterface)
                predicat = (t) => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(x => x == typeParent);

            if (predicat == null)
                throw new ArgumentException("в качестве типа-родителя необходимо указать тип класса либо интерфейса");

            var typeImplemented = Helper.AllCreatedTypeInDomain()
                .Where(predicat)
                .ToArray();

            typeForCreate.AddRange(typeImplemented);

            if (typeForCreate.Count == 0 && typeParent.IsClass && !typeParent.IsAbstract)
                typeForCreate.Add(typeParent);

            if (typeForCreate.Count == 0)
                throw new ArgumentException($"Из типа {typeParent.FullName} нельзя получить экземпляр объекта");

            var res = Array.CreateInstance(typeParent, typeForCreate.Count);

            for (int i = 0; i < typeForCreate.Count; i++)
                res.SetValue(Locator.GetInstance(typeForCreate[i]), i);

            return res;
        }
        /// <summary>
        /// Получить объект указанного типа
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetInstance<T>() where T : class
        {
            return (T)Locator.GetInstance(typeof(T));
        }
        /// <summary>
        /// Получить экземпляры объектов типа - наследника указанного
        /// </summary>
        /// <typeparam name="T">Тип-родитель</typeparam>
        /// <returns>Массив объектов типов-наследников</returns>
        public static T[] GetInstances<T>()
        {
            return Locator.GetInstances(typeof(T)).OfType<T>().ToArray();
        }

        /// <summary>
        /// Хелпер для локатора
        /// </summary>
        internal static class Helper
        {
            private class AslyLoadsT
            {
                public Assembly Assembly { get; set; }
                public Boolean DefTypes { get; set; }
            }

            private static List<Type> cashTypes = new List<Type>();
            /// <summary>
            /// Получение всех типов (DefineTypes), которые присутствуют в текущем домене приложения. 
            /// За исключением сборок из GAC и сборок с ошибкой загрузки зависимости. Для них берется ExportedTypes 
            /// </summary>
            /// <returns></returns>
            public static ICollection<Type> AllCreatedTypeInDomain()
            {
                lock (cashTypes)
                {
                    if (cashTypes.Any())
                        return cashTypes;

                    var types = new List<Type>();

                    var aslyDom = AppDomain.CurrentDomain.GetAssemblies();

                    var excludeAssemblyNames = new List<AssemblyName>();
                    excludeAssemblyNames.AddRange(aslyDom.Where(x => x.GlobalAssemblyCache).Select(x => x.GetName()).ToArray());

                    var assemblyes = new List<AslyLoadsT>();
                    assemblyes.AddRange(aslyDom
                        .Where(x => !x.GlobalAssemblyCache)
                        .Select(x => new AslyLoadsT() { Assembly = x, DefTypes = true })
                        .ToArray());

                    var stop = false;
                    do
                    {
                        stop = true;
                        var referAss = assemblyes
                            .Where(x => x.DefTypes)
                            .Select(x => new { TAL = x, Assbl = x.Assembly, RefAss = x.Assembly.GetReferencedAssemblies() }).ToArray();

                        foreach (var ras in referAss)
                            foreach (var ras_item in ras.RefAss)
                            {
                                if (assemblyes.Any(x => AssemblyName.ReferenceMatchesDefinition(x.Assembly.GetName(), ras_item)))
                                    continue;

                                if (excludeAssemblyNames.Any(x => AssemblyName.ReferenceMatchesDefinition(x, ras_item)))
                                    continue;

                                var domAss = AppDomain.CurrentDomain.GetAssemblies();

                                var da = domAss.FirstOrDefault(x => AssemblyName.ReferenceMatchesDefinition(x.GetName(), ras_item));
                                if (da == null)
                                {
                                    try
                                    {
                                        da = Assembly.Load(ras_item);
                                    }
                                    catch
                                    {
                                        ras.TAL.DefTypes = false;
                                    }
                                }

                                if (da != null)
                                {
                                    if (da.GlobalAssemblyCache)
                                        excludeAssemblyNames.Add(da.GetName());
                                    else
                                        assemblyes.Add(new AslyLoadsT() { Assembly = da, DefTypes = true });
                                }

                                stop = false;
                            }
                    }
                    while (!stop);

                    types.AddRange(
                        assemblyes
                            .Where(x => x.DefTypes)
                            .SelectMany(x => x.Assembly.DefinedTypes.Select(y => y.AsType()))
                            .ToArray());

                    types.AddRange(
                        assemblyes
                            .Where(x => !x.DefTypes)
                            .SelectMany(x => x.Assembly.ExportedTypes)
                            .ToArray());

                    cashTypes = types
                        .Where(x => !x.IsAbstract)
                        .Where(x => !x.IsInterface)
                        .Where(x => !typeof(Delegate).IsAssignableFrom(x))
                        .Where(x => !typeof(Attribute).IsAssignableFrom(x))
                        .Where(x => !x.GenericTypeArguments.Any())
                        .Where(x => !x.GetTypeInfo().GenericTypeParameters.Any())
                        .Where(x => x.GetConstructors().Any())
                        .ToList();

                    return cashTypes;
                }
            }
        }
    }
}
