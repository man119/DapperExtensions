using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

using Dapper;

namespace DapperExtensions.SqlServerExt
{
    public static class SqlServerExt
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
                    sqls.InsertSql = string.Format("INSERT INTO [{0}]({1})VALUES({2});SELECT @@IDENTITY", sqls.TableName, FieldsExtKey, FieldsAtExtKey);
                    sqls.InsertIdentitySql = string.Format("SET IDENTITY_INSERT [{0}] ON;INSERT INTO [{0}]({1})VALUES({2});SET IDENTITY_INSERT [{0}] OFF", sqls.TableName, Fields, FieldsAt);
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
        public static dynamic Insert<T>(this IDbConnection conn, T entity, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
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
        public static int InsertIdentity<T>(this IDbConnection conn, T entity, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
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
        /// 根据主键返回实体
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static T GetById<T>(this IDbConnection conn, dynamic id, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(T));
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
        /// 根据主键ids返回实体列表
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static IEnumerable<T> GetByIds<T>(this IDbConnection conn, object ids, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(T));
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
                throw new ArgumentException("表" + sqls.TableName + "没有主键，无法GetById。");
            }
        }

        /// <summary>
        /// 返回整张表数据
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static IEnumerable<T> GetAll<T>(this IDbConnection conn, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(T));
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
        /// 根据主键返回任意类型实体
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static T GetById<Table,T>(this IDbConnection conn, dynamic id, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(Table));
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
        /// 根据主键ids返回任意类型实体列表
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static IEnumerable<T> GetByIds<Table,T>(this IDbConnection conn, object ids, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(Table));
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
                throw new ArgumentException("表" + sqls.TableName + "没有主键，无法GetById。");
            }
        }

        /// <summary>
        /// 返回整张表任意类型数据
        /// returnFields需要返回的列，用逗号隔开。默认null，返回所有列
        /// </summary>
        public static IEnumerable<T> GetAll<Table,T>(this IDbConnection conn, string returnFields = null, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            DapperExtSqls sqls = GetDapperExtSqls(typeof(Table));
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
        /// 根据主键删除数据
        /// </summary>
        public static int DeleteById<T>(this IDbConnection conn, dynamic id, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
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
        public static int DeleteByIds<T>(this IDbConnection conn, object ids, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
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
        public static int DeleteAll<T>(this IDbConnection conn, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
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
                    string updateList = DapperExtCommon.GetFieldsEqStr(updateFields.Split(',').ToList(), "[", "]");
                    string sql = string.Format("UPDATE [{0}] SET {1} WHERE [{2}]=@{2}", sqls.TableName, updateList, sqls.KeyName);
                    return conn.Execute(sql, entity, transaction, commandTimeout);
                }
            }
            else
            {
                throw new ArgumentException("表" + sqls.TableName + "没有主键，无法UpdateById。");
            }
        }

    }
}
