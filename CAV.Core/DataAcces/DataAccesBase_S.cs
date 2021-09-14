using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Cav.DataAcces
{
    /// <summary>
    /// Базовый клас для адаптера на получение данных
    /// </summary>
    /// <typeparam name="Trow">Класс, на который производится отражение данных из БД</typeparam>
    /// <typeparam name="TselectParams">Клас, типизирующий параметры адаптера на выборку</typeparam>
    public class DataAccesBase<Trow, TselectParams> : DataAccesBase, IDataAcces<Trow, TselectParams>
        where Trow : class, new()
        where TselectParams : IAdapterParametrs
    {
        /// <summary>
        /// Получение данных из БД с записью в класс Trow, либо в его наследники
        /// </summary>
        /// <typeparam name="THeritorType">Указание типа для оторажения данных. Должен быть Trow или его наследником </typeparam>
        /// <param name="selectParams">Выражение на основе типа парамеров адаптера на выборку. Если null, то всем параметров присваивается DbNull</param>
        /// <returns>Коллекция объектов типа THeritorType</returns>
        public IEnumerable<THeritorType> Get<THeritorType>(
                Expression<Action<TselectParams>> selectParams = null)
                where THeritorType : Trow, new()
        {
            this.Configured();
            List<THeritorType> res = new List<THeritorType>();

            DbCommand ExecCom = AddParamToCommand(CommandActionType.Select, selectParams);
            var resExe = FillTable(ExecCom);
            foreach (DataRow dbRow in resExe.Rows)
            {
                THeritorType row = (THeritorType)Activator.CreateInstance(typeof(THeritorType));
                Trow prow = (Trow)row;
                foreach (var ff in selectPropFieldMap)
                    ff.Value(prow, dbRow);
                res.Add(row);
            }

            return res.ToArray();
        }

        internal DbCommand AddParamToCommand(CommandActionType actionType, Expression paramsExpr, Trow obj = null)
        {
            AdapterConfig config = null;
            if (!comands.TryGetValue(actionType, out config))
                throw new NotImplementedException($"Команда для {actionType.ToString()} не настроена");

            DbCommand command = createCommand(config);

            String key = actionType.ToString();

            var paramValues = ParceParams(paramsExpr, obj);

            foreach (var item in commandParams.Where(x => x.Key.StartsWith(key)))
            {
                var prmCmd = createParametr(item.Value);
                Object val = null;
                String paramValKey = item.Key.Replace(key + " ", String.Empty);

                if (paramValues.TryGetValue(paramValKey, out val))
                {
                    paramValues.Remove(paramValKey);

                    if (item.Value.ConvetProperty != null)
                        val = item.Value.ConvetProperty(val);
                }

                if (val != null)
                {
                    var valType = val.GetType();
                    valType = Nullable.GetUnderlyingType(valType) ?? valType;
                    if (valType == typeof(DateTime))
                        val = new DateTimeOffset((DateTime)val).LocalDateTime;

                    prmCmd.Value = val;
                }

                command.Parameters.Add(prmCmd);
            }

            if (paramValues.Count > 0)
            {
                var lost = paramValues.First();
                throw new ArgumentException(String.Format("Для свойства '{0}' не настроено сопоставление в операции {1}", lost.Key, key));
            }

            return command;
        }

        private Dictionary<string, object> ParceParams(Expression paramsExpr, Trow obj)
        {
            Dictionary<string, object> res = new Dictionary<string, object>();
            if (paramsExpr == null)
                return res;

            parceCall(res, (paramsExpr as LambdaExpression).Body as MethodCallExpression, obj);

            return res;
        }

        private void parceCall(Dictionary<string, object> d, MethodCallExpression mc, Trow obj)
        {
            var nextExp = mc.Arguments[0] as MethodCallExpression;

            if (nextExp != null)
                parceCall(d, nextExp, obj);

            Expression paramExp = ((mc.Arguments[1] as UnaryExpression).Operand as LambdaExpression).Body;

            Expression valueExp = mc.Arguments[2];

            if (paramExp.NodeType == ExpressionType.Convert)
                paramExp = (paramExp as UnaryExpression).Operand;
            String name = (paramExp as MemberExpression).Member.Name;

            if (d.ContainsKey(name))
                throw new ArgumentException($"Свойство {name} описано более одного раза");

            Object val = null;
            if (obj == null)
                val = Expression.Lambda<Func<Object>>(valueExp).Compile()();
            else
                val = ((valueExp as UnaryExpression).Operand as LambdaExpression).Compile().DynamicInvoke(obj);

            d.Add(name, val);
        }

        /// <summary>
        /// Получение данных из БД с записью в класс Trow
        /// </summary>
        /// <param name="selectParams">Выражение на основе типа парамеров адаптера на выборку. Если null, то всем параметров присваивается DbNull</param>
        /// <returns>Коллекция объектов типа Trow</returns>
        public IEnumerable<Trow> Get(Expression<Action<TselectParams>> selectParams = null)
        {
            return Get<Trow>(selectParams);
        }

        bool flagConfigured = false;
        private Object lockObj = new object();
        internal void Configured()
        {
            lock (lockObj)
            {
                if (flagConfigured)
                    return;
                this.ConfigAcces();
                flagConfigured = true;
            }
        }

        /// <summary>
        /// В производном классе - конфигурция адаптера. 
        /// </summary>
        protected virtual void ConfigAcces()
        {

        }

        /// <summary>
        /// Сопоставление свойства класса отражения с полем результирующего набора данных
        /// </summary>
        /// <param name="property">Свойство класса</param>
        /// <param name="fieldName">Имя поля в результурующем наборе</param>
        /// <param name="convertProperty">Дополнительная функция преобразования</param>
        protected void MapSelectField<T>(Expression<Func<Trow, T>> property, String fieldName, Expression<Func<Object, T>> convertProperty = null)
        {
            MapSelectFieldInDictionary(selectPropFieldMap, property, fieldName, convertProperty);
        }

        internal void MapSelectFieldInDictionary<T>(ConcurrentDictionary<String, Action<Trow, DataRow>> d, Expression<Func<Trow, T>> property, String fieldName, Expression<Func<Object, T>> convertProperty = null)
        {
            if (fieldName.IsNullOrWhiteSpace())
                throw new ArgumentNullException("paramName. Имя параметра не может быть пустым, состоять из пробелов или быть null");

            if (property == null)
                throw new ArgumentNullException("property. Выражение для свойства не может быть null");

            var propBody = property.Body;
            if (propBody.NodeType != ExpressionType.MemberAccess)
                throw new ArgumentException("В выражении property возможен только доступ к свойству или полю класса");

            String paramName = (propBody as MemberExpression).Member.Name;

            if (d.Keys.Contains(paramName))
                throw new ArgumentException($"Для свойства {paramName} уже определна связка");

            Type typeT = typeof(T);
            var nullableType = Nullable.GetUnderlyingType(typeT);
            if (nullableType != null)
                typeT = nullableType;

            if (!HeplerDataAcces.IsCanMappedDbType(typeT))
            {
                if (convertProperty == null)
                    throw new ArgumentException($"Для типа {typeT.FullName} свойства {paramName}, связываемого с полем {fieldName}, должен быть указан конвертор");

                if (!typeT.IsArray && typeT.GetConstructor(new Type[] { }) == null)
                    throw new ArgumentException($"Тип {typeT.FullName} для свойства {paramName} должен иметь открытый конструктор без параметров");
            }

            var p_rowObg = property.Parameters.First();
            var p_dbRow = Expression.Parameter(typeof(DataRow));

            Delegate expConv = null;

            if (convertProperty != null)
                expConv = convertProperty.Compile();

            Expression fromfield =
                Expression.Call(
                    typeof(HeplerDataAcces).GetMethod(nameof(HeplerDataAcces.FromField), BindingFlags.Static | BindingFlags.NonPublic),
                    Expression.Constant(propBody.Type, typeof(Type)),
                    p_dbRow,
                    Expression.Constant(fieldName, fieldName.GetType()),
                    Expression.Constant(expConv, typeof(Delegate))
                    );

            Expression assToProp = Expression.Assign(propBody, Expression.Convert(fromfield, propBody.Type));

            Expression<Action<Trow, DataRow>> readfomfield = Expression.Lambda<Action<Trow, DataRow>>(assToProp, p_rowObg, p_dbRow);

            d.TryAdd(paramName, readfomfield.Compile());
        }

        /// <summary>
        /// Сопоставление свойств класса параметров адаптера с параметрами скрипта выборки
        /// </summary>
        /// <typeparam name="T">Тип свойства</typeparam> 
        /// <param name="property">Свойство</param>
        /// <param name="paramName">Имя параметра</param>
        /// <param name="typeParam">Тип параметра в БД</param>
        protected void MapSelectParam<T>(Expression<Func<TselectParams, T>> property, String paramName, DbType? typeParam = null)
        {
            MapParam<T>(CommandActionType.Select, property, paramName, typeParam);
        }

        internal void MapParam<T>(CommandActionType actionType, Expression property, String paramName, DbType? typeParam = null, Expression convertProperty = null)
        {
            DbType? paramType = typeParam;

            if (paramName.IsNullOrWhiteSpace())
                throw new ArgumentNullException("paramName. Имя параметра не может быть пустым, состоять из пробелов или быть null");

            if (property == null)
                throw new ArgumentNullException("property. Выражение для свойства не может быть null");

            Type typeT = typeof(T);

            if (!HeplerDataAcces.IsCanMappedDbType(typeT) && convertProperty == null)
                throw new ArgumentException($"Для типа {typeT.FullName} должен быть указан конвертор");

            if (convertProperty != null)
            {
                Expression body = (convertProperty as LambdaExpression).Body;
                if (body.NodeType == ExpressionType.Convert)
                    body = (body as UnaryExpression).Operand;

                if (!paramType.HasValue)
                    paramType = HeplerDataAcces.TypeMapDbType(body.Type);
            }

            var proprow = (property as LambdaExpression).Body;
            if (proprow.NodeType != ExpressionType.MemberAccess)
                throw new ArgumentException("В выражении property возможен только доступ к свойству или полю класса");

            String propName = (proprow as MemberExpression).Member.Name;
            String key = actionType.ToString() + " " + propName;
            if (commandParams.ContainsKey(key))
                throw new ArgumentException($"Для свойства {propName} уже указано сопоставление");

            DbParamSetting param = new DbParamSetting();
            param.ParamName = paramName;

            if (!paramType.HasValue)
                paramType = HeplerDataAcces.TypeMapDbType(proprow.Type);
            param.ParamType = paramType.Value;

            if (convertProperty != null)
            {
                Func<T, Object> convFunct = (convertProperty as Expression<Func<T, Object>>).Compile();
                param.ConvetProperty = x => convFunct((T)x);
            }

            commandParams.TryAdd(key, param);
        }

        /// <summary>
        /// Конфигурация адаптера команды
        /// </summary>
        /// <param name="config">Объект с настройками адаптера</param>
        protected void ConfigCommand(AdapterConfig config)
        {
            if (comands.ContainsKey(config.ActionType))
                throw new ArgumentException($"В адаптере уже определена команда с типом {config.ActionType.ToString()}");
            if (config.TextCommand.IsNullOrWhiteSpace())
                throw new ArgumentNullException("Текст команды не может быть пустым");

            comands.TryAdd(config.ActionType, config);
        }

        private DbCommand createCommand(AdapterConfig config)
        {
            var cmmnd = this.CreateCommandObject();
            cmmnd.CommandText = config.TextCommand;
            cmmnd.CommandTimeout = config.TimeoutCommand;

            switch (config.TypeCommand)
            {
                case DataAccesCommandType.Text:
                    cmmnd.CommandType = CommandType.Text;
                    break;
                case DataAccesCommandType.StoredProcedure:
                    cmmnd.CommandType = CommandType.StoredProcedure;
                    break;
            }

            return cmmnd;
        }

        private DbParameter createParametr(DbParamSetting paramSetting)
        {
            var res = this.DbProviderFactoryGet().CreateParameter();
            res.ParameterName = paramSetting.ParamName;
            res.DbType = paramSetting.ParamType;
            res.Value = DBNull.Value;
            return res;
        }

        /// <summary>
        /// Класс инкапсуляции настроек адаптера
        /// </summary>
        protected sealed class AdapterConfig
        {
            /// <summary>
            /// Класс инкапсуляции настроек адаптера
            /// </summary>
            public AdapterConfig()
            {
                TimeoutCommand = 15;
            }
            /// <summary>
            /// Текст команды
            /// </summary>
            public String TextCommand { get; set; }
            /// <summary>
            /// Таймаут команды. По умолчанию - 15
            /// </summary>
            public int TimeoutCommand { get; set; }
            /// <summary>
            /// Тип команды
            /// </summary>
            public DataAccesCommandType TypeCommand { get; set; }
            /// <summary>
            /// Тип действия команды
            /// </summary>
            public CommandActionType ActionType { get; set; }
        }

        /// <summary>
        /// Тип команды в адаптере
        /// </summary>
        protected enum DataAccesCommandType
        {
            /// <summary>
            /// Текстовая строка
            /// </summary>
            Text,
            /// <summary>
            /// Хранимая процедура
            /// </summary>
            StoredProcedure
        }

        /// <summary>
        /// Тип действия адаптера
        /// </summary>
        public enum CommandActionType
        {
            /// <summary>
            /// Выборка
            /// </summary>
            Select,
            /// <summary>
            /// Вставка
            /// </summary>
            Insert,
            /// <summary>
            /// Обновление
            /// </summary>
            Update,
            /// <summary>
            /// Удаление
            /// </summary>
            Delete
        }

        private ConcurrentDictionary<String, Action<Trow, DataRow>> selectPropFieldMap = new ConcurrentDictionary<string, Action<Trow, DataRow>>();
        private ConcurrentDictionary<String, DbParamSetting> commandParams = new ConcurrentDictionary<string, DbParamSetting>();
        private ConcurrentDictionary<CommandActionType, AdapterConfig> comands = new ConcurrentDictionary<CommandActionType, AdapterConfig>();
    }

    internal struct DbParamSetting
    {
        public String ParamName { get; set; }
        public DbType ParamType { get; set; }
        public Func<Object, Object> ConvetProperty { get; set; }
    }

    /// <summary>
    /// Интерфейс для параметров адаптеров
    /// </summary>
    public interface IAdapterParametrs { }
}
