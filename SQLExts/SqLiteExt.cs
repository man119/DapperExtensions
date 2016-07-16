using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

using Dapper;

namespace DapperExtensions.SqLiteExt
{
    public static class SqLiteExt
    {
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, DapperExtSqls> dapperExtsqlsDict = new ConcurrentDictionary<RuntimeTypeHandle, DapperExtSqls>();
        public static DapperExtSqls GetDapperExtSqls(Type t)
        {
            if (dapperExtsqlsDict.Keys.Contains(t.TypeHandle))
            {
                return dapperExtsqlsDict[t.TypeHandle];
            }
            else
            {
                DapperExtSqls sqls = DapperExtCommon.GetDapperExtSqls(t);

                string Fields = DapperExtCommon.GetFieldsStr(sqls.AllFieldList, "[", "]");
                string FieldsAt = DapperExtCommon.GetFieldsAtStr(sqls.AllFieldList);
                string FieldsEq = DapperExtCommon.GetFieldsEqStr(sqls.AllFieldList, "[", "]");

                string FieldsExtKey = DapperExtCommon.GetFieldsStr(sqls.ExceptKeyFieldList, "[", "]");
                string FieldsAtExtKey = DapperExtCommon.GetFieldsAtStr(sqls.ExceptKeyFieldList);
                string FieldsEqExtKey = DapperExtCommon.GetFieldsEqStr(sqls.ExceptKeyFieldList, "[", "]");

                sqls.AllFields = Fields;

                if (sqls.HasKey && sqls.IsIdentity) //有主键并且是自增
                {
                    sqls.InsertSql = string.Format("INSERT INTO [{0}]({1})VALUES({2});SELECT last_insert_rowid()", sqls.TableName, FieldsExtKey, FieldsAtExtKey);
                    sqls.InsertIdentitySql = string.Format("INSERT INTO [{0}]({1})VALUES({2})", sqls.TableName, Fields, FieldsAt);
                }
                else
                {
                    sqls.InsertSql = string.Format("INSERT INTO [{0}]({1})VALUES({2})", sqls.TableName, Fields, FieldsAt);
                }

                if (sqls.HasKey) //含有主键
                {
                    sqls.DeleteByIdSql = string.Format("DELETE FROM [{0}] WHERE [{1}]=@id", sqls.TableName, sqls.KeyName);
                    sqls.DeleteByIdsSql = string.Format("DELETE FROM [{0}] WHERE [{1}] IN @ids", sqls.TableName, sqls.KeyName);
                    sqls.GetByIdSql = string.Format("SELECT {0} FROM [{1}] WHERE [{2}]=@id", Fields, sqls.TableName, sqls.KeyName);
                    sqls.GetByIdsSql = string.Format("SELECT {0} FROM [{1}] WHERE [{2}] IN @ids", Fields, sqls.TableName, sqls.KeyName);
                    sqls.UpdateByIdSql = string.Format("UPDATE [{0}] SET {1} WHERE [{2}]=@{2}", sqls.TableName, FieldsEqExtKey, sqls.KeyName);
                }
                sqls.DeleteAllSql = string.Format("DELETE FROM [{0}]", sqls.TableName);
                sqls.GetAllSql = string.Format("SELECT {0} FROM [{1}]", Fields, sqls.TableName);

                dapperExtsqlsDict[t.TypeHandle] = sqls;
                return sqls;
            }
        }

        /// <summary>
        /// 新增
        /// </summary>
        public static dynamic Insert(this IDbConnection conn, object entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(entity.GetType());
            if (sqls.HasKey && sqls.IsIdentity)
            {
                return conn.ExecuteScalar<dynamic>(sqls.InsertSql, entity, transaction, commandTimeout);
            }
            else
            {
                return conn.Execute(sqls.InsertSql, entity, transaction, commandTimeout);
            }
        }

        /// <summary>
        /// 新增(插入自增键)
        /// </summary>
        public static int InsertIdentity(this IDbConnection conn, object entity, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(entity.GetType());
            if (sqls.HasKey && sqls.IsIdentity)
            {
                return conn.Execute(sqls.InsertIdentitySql, entity, transaction, commandTimeout);
            }
            else
            {
                throw new ArgumentException("表" + sqls.TableName + "没有自增键，无法进行InsertIdentity。");
            }
        }


        /// <summary>
        /// 根据主键返回实体Base
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        private static T GetByIdBase<T>(this IDbConnection conn, Type t, dynamic id, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(t);
            if (sqls.HasKey)
            {
                DynamicParameters dpar = new DynamicParameters();
                dpar.Add("@id", id);
                if (returnFields == null)
                {
                    return conn.QueryFirstOrDefault<T>(sqls.GetByIdSql, dpar, transaction, commandTimeout);
                }
                else
                {
                    string sql = string.Format("SELECT {0} FROM [{1}] WHERE [{2}]=@id", returnFields, sqls.TableName, sqls.KeyName);
                    return conn.QueryFirstOrDefault<T>(sql, dpar, transaction, commandTimeout);
                }
            }
            else
            {
                throw new ArgumentException("表" + sqls.TableName + "没有主键，无法GetById。");
            }
        }

        /// <summary>
        /// 根据主键返dynamic
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static dynamic GetByIdDynamic<T>(this IDbConnection conn, dynamic id, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(T));
            if (sqls.HasKey)
            {
                DynamicParameters dpar = new DynamicParameters();
                dpar.Add("@id", id);
                if (returnFields == null)
                {
                    return conn.QueryFirstOrDefault(sqls.GetByIdSql, dpar, transaction, commandTimeout);
                }
                else
                {
                    string sql = string.Format("SELECT {0} FROM [{1}] WHERE [{2}]=@id", returnFields, sqls.TableName, sqls.KeyName);
                    return conn.QueryFirstOrDefault(sql, dpar, transaction, commandTimeout);
                }
            }
            else
            {
                throw new ArgumentException("表" + sqls.TableName + "没有主键，无法GetByIdDynamic。");
            }
        }

        /// <summary>
        /// 根据主键返回实体
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static T GetById<T>(this IDbConnection conn, dynamic id, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return GetByIdBase<T>(conn, typeof(T), id, returnFields, transaction, commandTimeout);
        }

        /// <summary>
        /// 根据主键返回任意类型实体
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static T GetById<Table, T>(this IDbConnection conn, dynamic id, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return GetByIdBase<T>(conn, typeof(Table), id, returnFields, transaction, commandTimeout);
        }

        /// <summary>
        /// 根据主键ids返回实体列表
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        private static IEnumerable<T> GetByIdsBase<T>(this IDbConnection conn, Type t, object ids, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(t);
            if (sqls.HasKey)
            {
                DynamicParameters dpar = new DynamicParameters();
                dpar.Add("@ids", ids);
                if (returnFields == null)
                {
                    return conn.Query<T>(sqls.GetByIdsSql, dpar, transaction, true, commandTimeout);
                }
                else
                {
                    string sql = string.Format("SELECT {0} FROM [{1}] WHERE [{2}] IN @ids", returnFields, sqls.TableName, sqls.KeyName);
                    return conn.Query<T>(sql, dpar, transaction, true, commandTimeout);
                }
            }
            else
            {
                throw new ArgumentException("表" + sqls.TableName + "没有主键，无法GetByIds。");
            }
        }

        /// <summary>
        /// 根据主键ids返回dynamic列表
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static IEnumerable<dynamic> GetByIdsDynamic<T>(this IDbConnection conn, object ids, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(T));
            if (sqls.HasKey)
            {
                DynamicParameters dpar = new DynamicParameters();
                dpar.Add("@ids", ids);
                if (returnFields == null)
                {
                    return conn.Query(sqls.GetByIdsSql, dpar, transaction, true, commandTimeout);
                }
                else
                {
                    string sql = string.Format("SELECT {0} FROM [{1}] WHERE [{2}] IN @ids", returnFields, sqls.TableName, sqls.KeyName);
                    return conn.Query(sql, dpar, transaction, true, commandTimeout);
                }
            }
            else
            {
                throw new ArgumentException("表" + sqls.TableName + "没有主键，无法GetByIdsDynamic。");
            }
        }

        /// <summary>
        /// 根据主键ids返回任意类型实体列表
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static IEnumerable<T> GetByIds<T>(this IDbConnection conn, object ids, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return GetByIdsBase<T>(conn, typeof(T), ids, returnFields, transaction, commandTimeout);
        }

        /// <summary>
        /// 根据主键ids返回任意类型实体列表
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static IEnumerable<T> GetByIds<Table, T>(this IDbConnection conn, object ids, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return GetByIdsBase<T>(conn, typeof(Table), ids, returnFields, transaction, commandTimeout);
        }

        /// <summary>
        /// 返回整张表数据
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        private static IEnumerable<T> GetAllBase<T>(this IDbConnection conn, Type t, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(t);
            if (returnFields == null)
            {
                return conn.Query<T>(sqls.GetAllSql, null, transaction, true, commandTimeout);
            }
            else
            {
                string sql = string.Format("SELECT {0} FROM [{1}]", returnFields, sqls.TableName);
                return conn.Query<T>(sql, null, transaction, true, commandTimeout);
            }
        }

        /// <summary>
        /// 返回整张表数据
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static IEnumerable<dynamic> GetAllDynamic<T>(this IDbConnection conn, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(T));
            if (returnFields == null)
            {
                return conn.Query(sqls.GetAllSql, null, transaction, true, commandTimeout);
            }
            else
            {
                string sql = string.Format("SELECT {0} FROM [{1}]", returnFields, sqls.TableName);
                return conn.Query(sql, null, transaction, true, commandTimeout);
            }
        }

        /// <summary>
        /// 返回整张表数据
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static IEnumerable<T> GetAll<T>(this IDbConnection conn, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return GetAllBase<T>(conn, typeof(T), returnFields, transaction, commandTimeout);
        }

        /// <summary>
        /// 返回整张表任意类型数据
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static IEnumerable<T> GetAll<Table, T>(this IDbConnection conn, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return GetAllBase<T>(conn, typeof(Table), returnFields, transaction, commandTimeout);
        }

        /// <summary>
        /// 根据主键删除数据
        /// </summary>
        public static int DeleteById<T>(this IDbConnection conn, dynamic id, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(T));
            if (sqls.HasKey)
            {
                DynamicParameters dpar = new DynamicParameters();
                dpar.Add("@id", id);
                return conn.Execute(sqls.DeleteByIdSql, dpar, transaction, commandTimeout);
            }
            else
            {
                throw new ArgumentException("表" + sqls.TableName + "没有主键，无法DeleteById。");
            }
        }

        /// <summary>
        /// 根据主键批量删除数据
        /// </summary>
        public static int DeleteByIds<T>(this IDbConnection conn, object ids, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(T));
            if (sqls.HasKey)
            {
                DynamicParameters dpar = new DynamicParameters();
                dpar.Add("@ids", ids);
                return conn.Execute(sqls.DeleteByIdsSql, dpar, transaction, commandTimeout);
            }
            else
            {
                throw new ArgumentException("表" + sqls.TableName + "没有主键，无法DeleteById。");
            }
        }

        /// <summary>
        /// 删除整张表数据
        /// </summary>
        public static int DeleteAll<T>(this IDbConnection conn, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(T));
            return conn.Execute(sqls.DeleteAllSql, null, transaction, commandTimeout);
        }

        /// <summary>
        /// 根据主键修改数据
        /// </summary>
        public static int UpdateById(this IDbConnection conn, object entity, string updateFields = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(entity.GetType());
            if (sqls.HasKey)
            {
                if (updateFields == null)
                {
                    return conn.Execute(sqls.UpdateByIdSql, entity, transaction, commandTimeout);
                }
                else
                {
                    string updateList = DapperExtCommon.GetFieldsEqStr(updateFields.Split(',').ToList(), "`", "`");
                    string sql = string.Format("UPDATE [{0}] SET {1} WHERE [{2}]=@{2}", sqls.TableName, updateList, sqls.KeyName);
                    return conn.Execute(sql, entity, transaction, commandTimeout);
                }
            }
            else
            {
                throw new ArgumentException("表" + sqls.TableName + "没有主键，无法UpdateById。");
            }
        }

        /// <summary>
        /// 获取总数
        /// </summary>
        /// <returns></returns>
        public static dynamic GetTotal<T>(this IDbConnection conn, string where = null, object param = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(T));
            string sql = string.Format("SELECT COUNT(1) FROM [{0}] {1}", sqls.TableName, where);
            return conn.ExecuteScalar<dynamic>(sql, param, transaction, commandTimeout);
        }

        /// <summary>
        /// Base获取数据,使用skip 和take
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<T> GetBySkipBase<T>(this IDbConnection conn, Type t, int skip, int take, string returnFields = null, string where = null, object param = null, string orderBy = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(t);
            if (returnFields == null)
                returnFields = sqls.AllFields;

            if (orderBy == null)
            {
                if (sqls.HasKey)
                {
                    orderBy = string.Format("ORDER BY [{0}] DESC", sqls.KeyName);
                }
                else
                {
                    orderBy = string.Format("ORDER BY [{0}]", sqls.AllFieldList.First());
                }
            }

            string sql = string.Format("SELECT {0} FROM [{1}] {2} {3} LIMIT {4},{5}", returnFields, sqls.TableName, where, orderBy, skip, take);

            return conn.Query<T>(sql, param, transaction, true, commandTimeout);
        }

        /// <summary>
        /// 获取dynamic数据,使用skip 和take
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<dynamic> GetBySkipDynamic<T>(this IDbConnection conn, int skip, int take, string returnFields = null, string where = null, object param = null, string orderBy = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(T));
            if (returnFields == null)
                returnFields = sqls.AllFields;

            if (orderBy == null)
            {
                if (sqls.HasKey)
                {
                    orderBy = string.Format("ORDER BY [{0}] DESC", sqls.KeyName);
                }
                else
                {
                    orderBy = string.Format("ORDER BY [{0}]", sqls.AllFieldList.First());
                }
            }

            string sql = string.Format("SELECT {0} FROM [{1}] {2} {3} LIMIT {4},{5}", returnFields, sqls.TableName, where, orderBy, skip, take);
            return conn.Query(sql, param, transaction, true, commandTimeout);
        }

        /// <summary>
        /// 获取dynamic分页数据
        /// </summary>
        public static IEnumerable<dynamic> GetByPageIndexDynamic<T>(this IDbConnection conn, int pageIndex, int pageSize, string returnFields = null, string where = null, object param = null, string orderBy = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            int skip = 0;
            if (pageIndex > 0)
            {
                skip = (pageIndex - 1) * pageSize;
            }
            return GetBySkipDynamic<T>(conn, skip, pageSize, returnFields, where, param, orderBy, transaction, commandTimeout);
        }

        /// <summary>
        /// 获取数据,使用skip 和take
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<T> GetBySkip<T>(this IDbConnection conn, int skip, int take, string returnFields = null, string where = null, object param = null, string orderBy = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return GetBySkipBase<T>(conn, typeof(T), skip, take, returnFields, where, param, orderBy, transaction, commandTimeout);
        }

        /// <summary>
        /// 获取数据，使用skip 和take,返回任意类型数据
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<T> GetBySkip<Table, T>(this IDbConnection conn, int skip, int take, string returnFields = null, string where = null, object param = null, string orderBy = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            return GetBySkipBase<T>(conn, typeof(Table), skip, take, returnFields, where, param, orderBy, transaction, commandTimeout);
        }

        /// <summary>
        /// 获取分页数据
        /// </summary>
        public static IEnumerable<T> GetByPageIndex<T>(this IDbConnection conn, int pageIndex, int pageSize, string returnFields = null, string where = null, object param = null, string orderBy = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            int skip = 0;
            if (pageIndex > 0)
            {
                skip = (pageIndex - 1) * pageSize;
            }
            return GetBySkip<T>(conn, skip, pageSize, returnFields, where, param, orderBy, transaction, commandTimeout);
        }

        /// <summary>
        /// 获取分页数据,返回任意类型数据
        /// </summary>
        public static IEnumerable<T> GetByPageIndex<Table, T>(this IDbConnection conn, int pageIndex, int pageSize, string returnFields = null, string where = null, object param = null, string orderBy = null, IDbTransaction transaction = null, int? commandTimeout = null)
        {
            int skip = 0;
            if (pageIndex > 0)
            {
                skip = (pageIndex - 1) * pageSize;
            }
            return GetBySkip<Table, T>(conn, skip, pageSize, returnFields, where, param, orderBy, transaction, commandTimeout);
        }
    }
}
