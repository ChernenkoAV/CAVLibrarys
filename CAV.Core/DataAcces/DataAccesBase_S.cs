﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

namespace Cav.DataAcces
{
    /// <summary>
    /// Базовый клас для адаптера на получение данных
    /// </summary>
    /// <typeparam name="Trow">Класс, на который производится отражение данных из БД</typeparam>
    /// <typeparam name="TselectParams">Клас, типизирующий параметры адаптера на выборку</typeparam>
    public class DataAccesBase<Trow, TselectParams> : DataAccesBase
        where Trow : class
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
                where THeritorType : Trow
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
            DbCommand command = null;
            if (!comands.TryGetValue(actionType, out command))
                throw new NotImplementedException("команда для " + actionType.ToString() + " не настроена");

            command.Parameters.Clear();
            String key = actionType.ToString();
            foreach (var item in commandParams.Where(x => x.Key.StartsWith(key)))
                item.Value.Value = DBNull.Value;
            foreach (var param in ParceParams(paramsExpr, obj))
                commandParams[key + " " + param.Key].Value = param.Value ?? DBNull.Value;
            command.Parameters.AddRange(commandParams.Where(x => x.Key.StartsWith(key)).Select(x => x.Value).ToArray());
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
                throw new ArgumentException("Свойство " + name + " описано более одного раза");

            Object val = null;
            if (obj == null)
                val = Expression.Lambda<Func<Object>>(valueExp).Compile()();
            else
                val = ((valueExp as UnaryExpression).Operand as Expression<Func<Trow, Object>>).Compile()(obj);

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
        internal void Configured()
        {
            if (flagConfigured)
                return;
            this.ConfigAcces();
            flagConfigured = true;
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
        /// <param name="property"></param>
        /// <param name="fieldName"></param>
        protected void MapSelectField(Expression<Func<Trow, Object>> property, String fieldName)
        {
            if (fieldName.IsNullOrWhiteSpace())
                throw new ArgumentNullException("Не указано имя поля для сопоставления");
            if (property == null)
                throw new ArgumentNullException("Не указано свойство для сопоставления");

            MapSelectFieldInDictionary(selectPropFieldMap, property, fieldName);
        }

        internal void MapSelectFieldInDictionary(Dictionary<String, Action<Trow, DataRow>> d, Expression<Func<Trow, Object>> property, String fieldName)
        {
            var proprow = property.Body;
            if (proprow.NodeType == ExpressionType.Convert)
                proprow = (proprow as UnaryExpression).Operand as MemberExpression;

            String paramName = (proprow as MemberExpression).Member.Name;

            if (d.Keys.Contains(paramName))
                throw new ArgumentException("Для свойства " + paramName + " уже определна связка");

            var p_rowObg = property.Parameters.First();
            var p_dbRow = Expression.Parameter(typeof(DataRow));

            Type typeProperty = proprow.Type;

            var fromfield =
                Expression.Call(
                    typeof(HeplerDataAcces).GetMethod("FromField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static),
                    Expression.Constant(typeProperty, typeof(Type)),
                    p_dbRow,
                    Expression.Constant(fieldName, fieldName.GetType()));
            var assi = Expression.Assign(proprow, Expression.Convert(fromfield, typeProperty));

            Expression<Action<Trow, DataRow>> readfomfield = Expression.Lambda<Action<Trow, DataRow>>(assi, p_rowObg, p_dbRow);

            d.Add(paramName, readfomfield.Compile());
        }

        /// <summary>
        /// Сопоставление свойств класса параметров адаптера с параметрами скрипта выборки
        /// </summary>
        /// <param name="property">Свойство</param>
        /// <param name="paramName">Имя параметра</param>
        /// <param name="typeParam">Тип параметра в БД</param>
        protected void MapSelectParam(Expression<Func<TselectParams, Object>> property, String paramName, DbType? typeParam = null)
        {
            MapParam(CommandActionType.Select, property, paramName, typeParam);
        }

        internal void MapParam(CommandActionType actionType, Expression property, String paramName, DbType? typeParam = null)
        {
            if (paramName.IsNullOrWhiteSpace())
                throw new ArgumentNullException("Имя параметра не может быть пустым, состоять из пробелов или быть null");

            var proprow = (property as LambdaExpression).Body;
            if (proprow.NodeType == ExpressionType.Convert)
                proprow = (proprow as UnaryExpression).Operand as MemberExpression;

            String propName = (proprow as MemberExpression).Member.Name;
            String key = actionType.ToString() + " " + propName;
            if (commandParams.ContainsKey(key))
                throw new ArgumentException("Для свойства  " + propName + " уже указано сопостовление");

            var dbparam = this.CreateCommandObject().CreateParameter();
            dbparam.ParameterName = paramName;
            if (typeParam.HasValue)
                dbparam.DbType = typeParam.Value;
            else
                dbparam.DbType = HeplerDataAcces.TypeMapDbType(proprow.Type);
            commandParams.Add(key, dbparam);
        }

        /// <summary>
        /// Конфигурация адаптера команды
        /// </summary>
        /// <param name="config">Объект с настройками адаптера</param>
        protected void ConfigCommand(AdapterConfig config)
        {
            if (comands.ContainsKey(config.ActionType))
                throw new ArgumentException("В адаптере уже определена сомманда с типом " + config.ActionType.ToString());
            if (config.TextCommand.IsNullOrWhiteSpace())
                throw new ArgumentNullException("Текст команды не может быть пустым");

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

            comands.Add(config.ActionType, cmmnd);
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
            /// Таймаут команды. ПО умолчанию - 15
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

        private Dictionary<String, Action<Trow, DataRow>> selectPropFieldMap = new Dictionary<string, Action<Trow, DataRow>>();
        private Dictionary<String, DbParameter> commandParams = new Dictionary<string, DbParameter>();
        private Dictionary<CommandActionType, DbCommand> comands = new Dictionary<CommandActionType, DbCommand>();
    }

    /// <summary>
    /// Интерфейс для параметров адаптеров
    /// </summary>
    public interface IAdapterParametrs { }
}