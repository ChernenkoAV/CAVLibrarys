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

        private static ConcurrentDictionary<Type, Object> cacheObjects = new ConcurrentDictionary<Type, object>();

        private static Object getObjectFromCache(Type type)
        {
            cacheObjects.TryGetValue(type, out var res);
            return res;
        }

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

            if (typeInstance.IsInterface || typeInstance.IsAbstract)
            {
                var insAbsO = GetInstances(typeInstance);
                if (insAbsO.Length > 1)
                    throw new ArgumentException($"{(typeInstance.IsInterface ? "Интерфейс" : "Абстрактный класс")} {typeInstance.FullName} имеет более одной реализации");
                if (insAbsO.Length == 0)
                    throw new ArgumentException($"{(typeInstance.IsInterface ? "Интерфейс" : "Абстрактный класс")} {typeInstance.FullName} не имеет реализаций");
                return insAbsO.GetValue(0);
            }

            if (!typeInstance.IsClass)
                throw new ArgumentException($"{nameof(typeInstance)} {typeInstance.FullName} должен быть класс");

            var akaSingleton = typeInstance.GetCustomAttribute<AlwaysNewAttribute>() == null;

            object res = null;

            if (akaSingleton && typeInstance.IsClass)
            {
                res = getObjectFromCache(typeInstance);
                if (res != null)
                    return res;
            }

            puhStackAndCheckRecursion(typeInstance);

            var constructor = typeInstance.GetConstructors().OrderBy(x => x.GetParameters().Length).FirstOrDefault() ??
                throw new ArgumentOutOfRangeException($"У типа {typeInstance.FullName} нет открытого конструктора");

            var paramConstr = new List<object>();

            foreach (var constParam in constructor.GetParameters())
            {
                Object paramInstance = null;
                var paramType = constParam.ParameterType;

                if (paramType.IsArray)
                {
                    var typeInArray = paramType.GetElementType();
                    paramInstance = Convert.ChangeType(GetInstances(typeInArray), paramType);
                }
                else
                {
                    if (typeof(IEnumerable).IsAssignableFrom(paramType))
                        throw new ArgumentOutOfRangeException($"тип {typeInstance.FullName}. в конструкторе поддерживаются только массивы");
                }

                if (paramInstance == null)
                    paramInstance = GetInstance(paramType);

                paramConstr.Add(paramInstance);
            }

            res = constructor.Invoke(paramConstr.ToArray());

            if (akaSingleton)
                cacheObjects.TryAdd(res.GetType(), res);

            foreach (var propInfo in typeInstance.GetProperties())
            {
                if (propInfo.GetCustomAttribute<PropertyInjectAttribute>() == null)
                    continue;

                if (!propInfo.CanWrite)
                    throw new InvalidOperationException($"свойство {typeInstance.FullName}.{propInfo.Name} должно быть доступно для записи");

                propSetData.Value.Add(new PropSetDataT() { Property = propInfo, InstatnceObject = res });
            }

            popStack();

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

                var msg = $"{promDep.ToArray().Reverse().JoinValuesToString(" -> ")} -> {type}";
                throw new InvalidOperationException("Обнаружена рекурсивная зависимость: " + msg);
            }

            pathDependency.Value.Push(typeInstance.FullName);
        }

        /// <summary>
        /// Получить экземпляры объектов типа - наследника указанного. 
        /// Если наследники не найдены и родитель имеет открытый конструктор, то создается экземпляр родителя.
        /// </summary>
        /// <param name="typeParent">Тип-родитель(класс или интерфейс)</param>
        /// <returns>Массив экземпляров</returns>
        public static Array GetInstances(Type typeParent)
        {
            if (typeParent is null)
                throw new ArgumentNullException(nameof(typeParent));

            Func<Type, bool> predicat = null;

            if (typeParent.IsClass)
                predicat = (t) => t.IsSubclassOf(typeParent);

            if (typeParent.IsInterface)
                predicat = (t) => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(x => x == typeParent);

            if (predicat == null)
                throw new ArgumentException("в качестве типа-родителя необходимо указать тип класса либо интерфейса");

            var typeForCreate = CashTypesOnDomain.AllCreatedType
                .Where(predicat)
                .ToList();

            if (typeForCreate.Count == 0 &&
                typeParent.IsClass &&
                !typeParent.IsAbstract &&
                typeParent.GetConstructor(Array.Empty<Type>()) != null)
                typeForCreate.Add(typeParent);

            var res = Array.CreateInstance(typeParent, typeForCreate.Count);

            for (var i = 0; i < typeForCreate.Count; i++)
                res.SetValue(GetInstance(typeForCreate[i]), i);

            return res;
        }
        /// <summary>
        /// Получить объект указанного типа
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetInstance<T>() where T : class => (T)GetInstance(typeof(T));
        /// <summary>
        /// Получить экземпляры объектов типа - наследника указанного
        /// </summary>
        /// <typeparam name="T">Тип-родитель</typeparam>
        /// <returns>Массив объектов типов-наследников</returns>
        public static T[] GetInstances<T>() => GetInstances(typeof(T)).Cast<T>().ToArray();

        /// <summary>
        /// Хелпер для локатора
        /// </summary>
        internal static class CashTypesOnDomain
        {
            private static Lazy<List<Type>> cashTypes = new Lazy<List<Type>>(valueFactory: allCreatedTypeInDomain, mode: LazyThreadSafetyMode.ExecutionAndPublication);

            public static ICollection<Type> AllCreatedType => cashTypes.Value;
            /// <summary>
            /// Получение всех типов (DefineTypes), которые присутствуют в текущем домене приложения. 
            /// За исключением сборок из GAC и сборок с ошибкой загрузки зависимости. Для них берется ExportedTypes 
            /// </summary>
            /// <returns></returns>
            private static List<Type> allCreatedTypeInDomain()
            {

                #region Прогружаем референсные сборки в домен приложения

#pragma warning disable IDE0039 // Использовать локальную функцию
                Action<Assembly> recursionLoadAssembly = null;
#pragma warning restore IDE0039 // Использовать локальную функцию
                recursionLoadAssembly = asbly =>
                {
                    var referAss = asbly
                        .GetReferencedAssemblies()
                        .Where(an => !AppDomain.CurrentDomain.GetAssemblies().Any(gan => AssemblyName.ReferenceMatchesDefinition(gan.GetName(), an)))
                        .ToList();

                    foreach (var rAsbl in referAss)
                    {
                        try
                        {
                            var lAs = Assembly.Load(rAsbl);
                            recursionLoadAssembly(lAs);
                        }
                        catch { }
                    }
                };

                foreach (var aitem in AppDomain.CurrentDomain.GetAssemblies().ToArray())
                    recursionLoadAssembly(aitem);

                #endregion

                var assemblysForWork = AppDomain.CurrentDomain.GetAssemblies().ToArray();

                var res = new List<Type>();

#pragma warning disable IDE0039 // Использовать локальную функцию
                Func<IEnumerable<Type>, List<Type>> filterTypes = inLi => inLi
                    .Where(x =>
                        !x.IsAbstract &&
                        !x.IsInterface &&
                        !typeof(Delegate).IsAssignableFrom(x) &&
                        !typeof(Attribute).IsAssignableFrom(x) &&
                        !x.GenericTypeArguments.Any() &&
                        !x.GetTypeInfo().GenericTypeParameters.Any() &&
                        x.GetConstructors().Any())
                    .ToList();
#pragma warning restore IDE0039 // Использовать локальную функцию

                foreach (var aitem in assemblysForWork)
                {
                    try
                    {
                        res.AddRange(filterTypes(aitem.DefinedTypes.Select(x => x.AsType()).ToArray()));
                    }
                    catch
                    {
                        try
                        {
                            res.AddRange(filterTypes(aitem.ExportedTypes));
                        }
                        catch
                        {

                        }
                    }
                }

                return res;
            }
        }
    }
}
