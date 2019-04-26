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
    /// Локатор-контейнер объектов
    /// </summary>
    public static class Locator
    {
        /// <summary>
        /// Дополнительные действия с объектом после создания
        /// </summary>
        public static Action<Object> AdditionalSettingsObject { get; set; }
        static Locator()
        {
            UseCache = true;
        }

        private static ConcurrentDictionary<Type, Object> cacheObjects = new ConcurrentDictionary<Type, object>();

        private static Object GetObjectFromCache(Type type)
        {
            Object res = null;
            cacheObjects.TryGetValue(type, out res);
            return res;
        }

        private static void PutObjectToCache(Object inst)
        {
            Type typrObj = inst.GetType();
            cacheObjects.TryAdd(typrObj, inst);
        }
        /// <summary>
        /// Использовать кэш объектов (false - объект и зависимости создаются заново)
        /// </summary>
        public static Boolean UseCache { get; set; }
        /// <summary>
        ///Получить экземпляр указанного типа 
        /// </summary>
        /// <param name="typeInstance"></param>
        /// <returns></returns>
        public static object GetInstance(Type typeInstance)
        {
            if (typeInstance == null)
                throw new ArgumentNullException("typeInstance");

            if (typeInstance.IsValueType)
                return Activator.CreateInstance(typeInstance);

            if (!typeInstance.IsClass)
                throw new ArgumentException("typeInstance должен быть класс или интерфейс");

            if (typeInstance.IsAbstract)
                throw new ArgumentException("typeInstance не может бать абстрактным классом");

            object res = null;

            if (UseCache && typeInstance.IsClass)
            {
                res = GetObjectFromCache(typeInstance);
                if (res != null)
                    return res;
            }

            PuhStackAndCheckRecursion(typeInstance);

            var constructor = typeInstance.GetConstructors().OrderBy(x => x.GetParameters().Length).FirstOrDefault();
            if (constructor == null)
                throw new ArgumentOutOfRangeException(String.Format("у типа {0} нет открытого конструктора", typeInstance.FullName));

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
                        throw new ArgumentException(String.Format("Интерфейс {0} имеет более одной реализации", paramType.FullName));
                    if (instsesInterface.Length == 0)
                        throw new ArgumentException(String.Format("Интерфейс {0} не имеет реализаций", paramType.FullName));
                    paramInstance = instsesInterface.GetValue(0);
                }

                if (paramInstance == null)
                    paramInstance = Locator.GetInstance(paramType);

                paramConstr.Add(paramInstance);
            }

            res = constructor.Invoke(paramConstr.ToArray());

            AdditionalSettingsObject?.Invoke(res);

            if (UseCache)
                PutObjectToCache(res);

            foreach (var propInfo in typeInstance.GetProperties())
            {
                if (propInfo.GetCustomAttribute<PropertyInjectAttribute>() == null)
                    continue;

                if (!propInfo.CanWrite)
                    throw new InvalidOperationException($"свойство {typeInstance.FullName}.{propInfo.Name} должно быть доступно для записи");

                propSetData.Value.Add(new PropSetDataT() { Property = propInfo, InstatnceObject = res });
            }

            PopStack();

            return res;
        }


        private static ThreadLocal<Stack<String>> pathDependency = new ThreadLocal<Stack<string>>(() => new Stack<string>());
        private static ThreadLocal<List<PropSetDataT>> propSetData = new ThreadLocal<List<PropSetDataT>>(() => new List<PropSetDataT>());

        private static void PopStack()
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

        private static void PuhStackAndCheckRecursion(Type typeInstance)
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

            TypeInfo[] allTypeInDomain = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.DefinedTypes).ToArray();

            Func<TypeInfo, bool> predicat = null;

            if (typeParent.IsClass)
                predicat = (t) => t.IsSubclassOf(typeParent);

            if (typeParent.IsInterface)
                predicat = (t) => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(x => x == typeParent);

            if (predicat == null)
                throw new ArgumentException("в качестве типа-родителя необходимо указать тип класса либо интерфейса");

            var typeImplemented = allTypeInDomain
                .Where(predicat)
                .Select(x => x.AsType())
                .ToArray();

            typeForCreate.AddRange(typeImplemented);

            if (typeForCreate.Count == 0 && typeParent.IsClass && !typeParent.IsAbstract)
                typeForCreate.Add(typeParent);

            if (typeForCreate.Count == 0)
                throw new ArgumentException(String.Format("Из типа {0} нельзя получить экземпляр объекта", typeParent.FullName));

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
    }

    internal class PropSetDataT
    {
        public PropertyInfo Property { get; set; }
        public Object InstatnceObject { get; set; }
    }
}
