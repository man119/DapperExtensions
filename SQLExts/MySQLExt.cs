using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DapperExtensions.MySQLExt
{
    public static class MySQLExt
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

                string Fields = DapperExtCommon.GetFieldsStr(sqls.AllFieldList, "`", "`");
                string FieldsAt = DapperExtCommon.GetFieldsAtStr(sqls.AllFieldList);
                string FieldsEq = DapperExtCommon.GetFieldsEqStr(sqls.AllFieldList, "`", "`");

                string FieldsExtKey = DapperExtCommon.GetFieldsStr(sqls.ExceptKeyFieldList, "`", "`");
                string FieldsAtExtKey = DapperExtCommon.GetFieldsAtStr(sqls.ExceptKeyFieldList);
                string FieldsEqExtKey = DapperExtCommon.GetFieldsEqStr(sqls.ExceptKeyFieldList, "`", "`");

                sqls.AllFields = Fields;

                if (sqls.HasKey && sqls.IsIdentity) //有主键并且是自增
                {
                    sqls.InsertSql = string.Format("INSERT INTO `{0}`({1})VALUES({2});SELECT @@IDENTITY", sqls.TableName, FieldsExtKey, FieldsAtExtKey);
                    sqls.InsertIdentitySql = string.Format("INSERT INTO `{0}`({1})VALUES({2})", sqls.TableName, Fields, FieldsAt);
                }
                else
                {
                    sqls.InsertSql = string.Format("INSERT INTO `{0}`({1})VALUES({2})", sqls.TableName, Fields, FieldsAt);
                }

                if (sqls.HasKey) //含有主键
                {
                    sqls.DeleteByIdSql = string.Format("DELETE FROM `{0}` WHERE `{1}`=@id", sqls.TableName, sqls.KeyName);
                    sqls.DeleteByIdsSql = string.Format("DELETE FROM `{0}` WHERE `{1}` IN @ids", sqls.TableName, sqls.KeyName);
                    sqls.GetByIdSql = string.Format("SELECT {0} FROM `{1}` WHERE `{2}`=@id", Fields, sqls.TableName, sqls.KeyName);
                    sqls.GetByIdsSql = string.Format("SELECT {0} FROM `{1}` WHERE `{2}` IN @ids", Fields, sqls.TableName, sqls.KeyName);
                    sqls.UpdateByIdSql = string.Format("UPDATE `{0}` SET {1} WHERE `{2}`=@{2}", sqls.TableName, FieldsEqExtKey, sqls.KeyName);
                }
                sqls.DeleteAllSql = string.Format("DELETE FROM `{0}`", sqls.TableName);
                sqls.GetAllSql = string.Format("SELECT {0} FROM `{1}`", Fields, sqls.TableName);

                dapperExtsqlsDict[t.TypeHandle] = sqls;
                return sqls;
            }
        }
    }
}
