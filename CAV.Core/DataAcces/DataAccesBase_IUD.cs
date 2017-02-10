using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;

namespace Cav.DataAcces
{
    /// <summary>
    /// Базовый класс адаптера получения и изменения данных в БД.
    /// </summary>
    /// <typeparam name="Trow">Класс, на который производится отражение данных из БД</typeparam>
    /// <typeparam name="TselectParams">Клас, типизирующий параметры адаптера на выборку</typeparam>
    /// <typeparam name="TupdateParams">Клас, типизирующий параметры адаптера на изменение</typeparam>
    /// <typeparam name="TdeleteParams">Клас, типизирующий параметры адаптера на удаление</typeparam>
    public class DataAccesBase<Trow, TselectParams, TupdateParams, TdeleteParams> : DataAccesBase<Trow, TselectParams>
        where Trow : class
        where TselectParams : IAdapterParametrs
        where TupdateParams : IAdapterParametrs
        where TdeleteParams : IAdapterParametrs
    {
        #region Add

        /// <summary>
        /// Добавить объект в БД
        /// </summary>
        /// <param name="newObj">Экземпляр объекта, который необходимо добавит в БД</param>
        public void Add(Trow newObj)
        {
            this.Configured();

            DbCommand execCom = AddParamToCommand(CommandActionType.Insert, insertExpression, newObj);
            var resExec = FillTable(execCom);
            foreach (DataRow dbrow in resExec.Rows)
                foreach (var ff in insertPropKeyFieldMap)
                    ff.Value(newObj, dbrow);
        }


        private Dictionary<String, Action<Trow, DataRow>> insertPropKeyFieldMap = new Dictionary<string, Action<Trow, DataRow>>();
        private LambdaExpression insertExpression = null;


        /// <summary>
        /// Сопоставление свойства объекта отражения и имени параметра адаптера
        /// </summary>
        /// <typeparam name="T">Тип свойства</typeparam>
        /// <param name="property">Свойство</param>
        /// <param name="paramName">Имя параметра</param>
        /// <param name="typeParam">Тип параметра в БД</param>
        /// <param name="addedConvertFunct">Дополнительная функция преобразования</param> 
        protected void MapInsertParam<T>(Expression<Func<Trow, T>> property, String paramName, DbType? typeParam = null, Expression<Func<T, Object>> addedConvertFunct = null)
        {
            MapParam<T>(CommandActionType.Insert, property, paramName, typeParam, addedConvertFunct);

            var paramExp = property.Parameters[0];

            Expression insadeCall = null;

            if (insertExpression != null)
                insadeCall = ParameterRebinder.ReplaceParameters(property, insertExpression);

            Expression metodCall =
                    Expression.Call(
                        typeof(HeplerDataAcces),
                        nameof(HeplerDataAcces.ForParceParams),
                        new Type[] { typeof(Trow) },
                        insadeCall ?? paramExp,
                        Expression.Quote(property),
                        Expression.Quote(property));

            insertExpression = Expression.Lambda<Action<Trow>>(
                metodCall,
                paramExp);
        }

        /// <summary>
        /// Сопоставление полей объекта отражения данных и возврата ключей послк операции вставки данных
        /// </summary>
        /// <param name="property"></param>
        /// <param name="fieldName"></param>
        protected void MapInsertKeyParam<T>(Expression<Func<Trow, T>> property, String fieldName)
        {
            MapSelectFieldInDictionary(insertPropKeyFieldMap, property, fieldName);
        }

        #endregion

        #region Delete

        /// <summary>
        /// Удаление по предикату 
        /// </summary>
        /// <param name="deleteParams"></param>
        public void Delete(Expression<Action<TdeleteParams>> deleteParams)
        {
            this.Configured();

            DbCommand execCom = AddParamToCommand(CommandActionType.Delete, deleteParams);
            ExecuteNonQuery(execCom);
        }

        /// <summary>
        /// Сопоставление объекта параметров удаления и параметров адаптера удаления
        /// </summary>
        /// <typeparam name="T">Тип свойства</typeparam>
        /// <param name="property">Свойство</param>
        /// <param name="paramName">Имя параметра</param>
        /// <param name="typeParam">Тип параметра в БД</param>
        protected void MapDeleteParam<T>(Expression<Func<TdeleteParams, T>> property, String paramName, DbType? typeParam = null)
        {
            MapParam<T>(CommandActionType.Delete, property, paramName, typeParam);
        }

        #endregion

        #region Update

        /// <summary>
        /// Обновление данных
        /// </summary>
        /// <param name="updateParams"></param>
        public void Update(Expression<Action<TupdateParams>> updateParams)
        {
            this.Configured();

            DbCommand execCom = AddParamToCommand(CommandActionType.Update, updateParams);
            ExecuteNonQuery(execCom);
        }

        /// <summary>
        /// Сопоставление свойств класса параметров обновления и параметров адаптера
        /// </summary>
        /// <typeparam name="T">Тип свойства</typeparam>
        /// <param name="property">Свойство</param>
        /// <param name="paramName">Имя параметра</param>
        /// <param name="typeParam">Тип параметра в БД</param>
        /// <param name="addedConvertFunct">Дополнительная функция преобразования</param>
        protected void MapUpdateParam<T>(Expression<Func<TupdateParams, T>> property, String paramName, DbType? typeParam = null, Expression<Func<T, Object>> addedConvertFunct = null)
        {
            MapParam<T>(CommandActionType.Update, property, paramName, typeParam, addedConvertFunct);
        }

        #endregion

    }
}
