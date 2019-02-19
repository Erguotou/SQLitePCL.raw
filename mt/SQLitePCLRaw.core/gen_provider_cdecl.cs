﻿/*
   Copyright 2014-2019 Zumero, LLC

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

// Copyright © Microsoft Open Technologies, Inc.
// All Rights Reserved
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT.
// 
// See the Apache 2 License for the specific language governing permissions and limitations under the License.

namespace SQLitePCL
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
	using System.Reflection;

	[Preserve(AllMembers = true)]
    sealed class SQLite3Provider_Cdecl : ISQLite3Provider
    {
		const CallingConvention CALLING_CONVENTION = CallingConvention.Cdecl;

		public static void Setup(IGetFunctionPtr gf)
		{
			NativeMethods.Setup(gf);
		}

        int ISQLite3Provider.sqlite3_win32_set_directory(int typ, string path)
        {
            return NativeMethods.sqlite3_win32_set_directory((uint) typ, path);
        }

        int ISQLite3Provider.sqlite3_open(string filename, out IntPtr db)
        {
            return NativeMethods.sqlite3_open(util.to_utf8(filename), out db);
        }

        int ISQLite3Provider.sqlite3_open_v2(string filename, out IntPtr db, int flags, string vfs)
        {
            return NativeMethods.sqlite3_open_v2(util.to_utf8(filename), out db, flags, util.to_utf8(vfs));
        }

    #pragma warning disable 649
    private struct sqlite3_vfs
    {
        public int iVersion;
        public int szOsFile;
        public int mxPathname;
        public IntPtr pNext;
        public IntPtr zName;
        public IntPtr pAppData;
        public IntPtr xOpen;
        public SQLiteDeleteDelegate xDelete;
        public IntPtr xAccess;
        public IntPtr xFullPathname;
        public IntPtr xDlOpen;
        public IntPtr xDlError;
        public IntPtr xDlSym;
        public IntPtr xDlClose;
        public IntPtr xRandomness;
        public IntPtr xSleep;
        public IntPtr xCurrentTime;
        public IntPtr xGetLastError;

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int SQLiteDeleteDelegate(IntPtr pVfs, byte[] zName, int syncDir);
    }
    #pragma warning restore 649
	
	int ISQLite3Provider.sqlite3__vfs__delete(string vfs, string filename, int syncDir)
	{
	    IntPtr ptrVfs = NativeMethods.sqlite3_vfs_find(util.to_utf8(vfs));
	    // this code and the struct it uses was taken from aspnet/DataCommon.SQLite, Apache License 2.0
	    sqlite3_vfs vstruct = (sqlite3_vfs) Marshal.PtrToStructure(ptrVfs, typeof(sqlite3_vfs));
	    return vstruct.xDelete(ptrVfs, util.to_utf8(filename), 1);
	}

        int ISQLite3Provider.sqlite3_close_v2(IntPtr db)
        {
            var rc = NativeMethods.sqlite3_close_v2(db);
		hooks.removeFor(db);
		return rc;
        }

        int ISQLite3Provider.sqlite3_close(IntPtr db)
        {
            var rc = NativeMethods.sqlite3_close(db);
		hooks.removeFor(db);
		return rc;
        }

        int ISQLite3Provider.sqlite3_enable_shared_cache(int enable)
        {
            return NativeMethods.sqlite3_enable_shared_cache(enable);
        }

        void ISQLite3Provider.sqlite3_interrupt(IntPtr db)
        {
            NativeMethods.sqlite3_interrupt(db);
        }

        [MonoPInvokeCallback (typeof(NativeMethods.callback_exec))]
        static int exec_hook_bridge(IntPtr p, int n, IntPtr values_ptr, IntPtr names_ptr)
        {
            exec_hook_info hi = exec_hook_info.from_ptr(p);
            return hi.call(n, values_ptr, names_ptr);
        }
// TODO shouldn't there be a impl/bridge thing here

        int ISQLite3Provider.sqlite3_exec(IntPtr db, string sql, delegate_exec func, object user_data, out string errMsg)
        {
            IntPtr errmsg_ptr;
            int rc;

            if (func != null)
            {
                exec_hook_info hi = new exec_hook_info(func, user_data);
                rc = NativeMethods.sqlite3_exec(db, util.to_utf8(sql), exec_hook_bridge, hi.ptr, out errmsg_ptr);
                hi.free();
            }
            else
            {
                rc = NativeMethods.sqlite3_exec(db, util.to_utf8(sql), null, IntPtr.Zero, out errmsg_ptr);
            }

            if (errmsg_ptr == IntPtr.Zero)
            {
                errMsg = null;
            }
            else
            {
                errMsg = util.from_utf8(errmsg_ptr);
                NativeMethods.sqlite3_free(errmsg_ptr);
            }

            return rc;
        }

        int ISQLite3Provider.sqlite3_complete(string sql)
        {
            return NativeMethods.sqlite3_complete(util.to_utf8(sql));
        }

        string ISQLite3Provider.sqlite3_compileoption_get(int n)
        {
            return util.from_utf8(NativeMethods.sqlite3_compileoption_get(n));
        }

        int ISQLite3Provider.sqlite3_compileoption_used(string s)
        {
            return NativeMethods.sqlite3_compileoption_used(util.to_utf8(s));
        }

        int ISQLite3Provider.sqlite3_table_column_metadata(IntPtr db, string dbName, string tblName, string colName, out string dataType, out string collSeq, out int notNull, out int primaryKey, out int autoInc)
        {
            IntPtr datatype_ptr;
            IntPtr collseq_ptr;

            int rc = NativeMethods.sqlite3_table_column_metadata(
                        db, util.to_utf8(dbName), util.to_utf8(tblName), util.to_utf8(colName), 
                        out datatype_ptr, out collseq_ptr, out notNull, out primaryKey, out autoInc);

            if (datatype_ptr == IntPtr.Zero)
            {
                dataType = null;
            }
            else
            {
                dataType = util.from_utf8(datatype_ptr);
                if (dataType.Length == 0)
                {
                    dataType = null;
                }
            }

            if (collseq_ptr == IntPtr.Zero)
            {
                collSeq = null;
            }
            else
            {
                collSeq = util.from_utf8(collseq_ptr);
                if (collSeq.Length == 0)
                {
                    collSeq = null;
                }
            }         
  
            return rc; 
        }

        int ISQLite3Provider.sqlite3_prepare_v2(IntPtr db, string sql, out IntPtr stm, out string remain)
        {
            var ba_sql = util.to_utf8(sql);
            GCHandle pinned_sql = GCHandle.Alloc(ba_sql, GCHandleType.Pinned);
            IntPtr ptr_sql = pinned_sql.AddrOfPinnedObject();
            IntPtr tail;
            int rc = NativeMethods.sqlite3_prepare_v2(db, ptr_sql, -1, out stm, out tail);
            if (tail == IntPtr.Zero)
            {
                remain = null;
            }
            else
            {
                remain = util.from_utf8(tail);
                if (remain.Length == 0)
                {
                    remain = null;
                }
            }
            pinned_sql.Free();
            return rc;
        }

        int ISQLite3Provider.sqlite3_db_status(IntPtr db, int op, out int current, out int highest, int resetFlg)
        {
            return NativeMethods.sqlite3_db_status(db, op, out current, out highest, resetFlg);
        }

        string ISQLite3Provider.sqlite3_sql(IntPtr stmt)
        {
            return util.from_utf8(NativeMethods.sqlite3_sql(stmt));
        }

        IntPtr ISQLite3Provider.sqlite3_db_handle(IntPtr stmt)
        {
            return NativeMethods.sqlite3_db_handle(stmt);
        }

        int ISQLite3Provider.sqlite3_blob_open(IntPtr db, byte[] db_utf8, byte[] table_utf8, byte[] col_utf8, long rowid, int flags, out IntPtr blob)
        {
            return NativeMethods.sqlite3_blob_open(db, db_utf8, table_utf8, col_utf8, rowid, flags, out blob);
        }

        int ISQLite3Provider.sqlite3_blob_open(IntPtr db, string sdb, string table, string col, long rowid, int flags, out IntPtr blob)
        {
            return NativeMethods.sqlite3_blob_open(db, util.to_utf8(sdb), util.to_utf8(table), util.to_utf8(col), rowid, flags, out blob);
        }

        int ISQLite3Provider.sqlite3_blob_bytes(IntPtr blob)
        {
            return NativeMethods.sqlite3_blob_bytes(blob);
        }

        int ISQLite3Provider.sqlite3_blob_close(IntPtr blob)
        {
            return NativeMethods.sqlite3_blob_close(blob);
        }

        int ISQLite3Provider.sqlite3_blob_read(IntPtr blob, byte[] b, int n, int offset)
        {
            return (this as ISQLite3Provider).sqlite3_blob_read(blob, b, 0, n, offset);
        }

        int ISQLite3Provider.sqlite3_blob_write(IntPtr blob, byte[] b, int n, int offset)
        {
            return (this as ISQLite3Provider).sqlite3_blob_write(blob, b, 0, n, offset);
        }

        int ISQLite3Provider.sqlite3_blob_read(IntPtr blob, byte[] b, int bOffset, int n, int offset)
        {
            GCHandle pinned = GCHandle.Alloc(b, GCHandleType.Pinned);
            IntPtr ptr = pinned.AddrOfPinnedObject();
            int rc = NativeMethods.sqlite3_blob_read(blob, new IntPtr(ptr.ToInt64() + bOffset), n, offset);
            pinned.Free();
	    return rc;
        }

        int ISQLite3Provider.sqlite3_blob_write(IntPtr blob, byte[] b, int bOffset, int n, int offset)
        {
            GCHandle pinned = GCHandle.Alloc(b, GCHandleType.Pinned);
            IntPtr ptr = pinned.AddrOfPinnedObject();
            int rc = NativeMethods.sqlite3_blob_write(blob, new IntPtr(ptr.ToInt64() + bOffset), n, offset);
            pinned.Free();
	    return rc;
        }

        IntPtr ISQLite3Provider.sqlite3_backup_init(IntPtr destDb, string destName, IntPtr sourceDb, string sourceName)
        {
            return NativeMethods.sqlite3_backup_init(destDb, util.to_utf8(destName), sourceDb, util.to_utf8(sourceName));
        }

        int ISQLite3Provider.sqlite3_backup_step(IntPtr backup, int nPage)
        {
            return NativeMethods.sqlite3_backup_step(backup, nPage);
        }

        int ISQLite3Provider.sqlite3_backup_finish(IntPtr backup)
        {
            return NativeMethods.sqlite3_backup_finish(backup);
        }

        int ISQLite3Provider.sqlite3_backup_remaining(IntPtr backup)
        {
            return NativeMethods.sqlite3_backup_remaining(backup);
        }

        int ISQLite3Provider.sqlite3_backup_pagecount(IntPtr backup)
        {
            return NativeMethods.sqlite3_backup_pagecount(backup);
        }

        IntPtr ISQLite3Provider.sqlite3_next_stmt(IntPtr db, IntPtr stmt)
        {
            return NativeMethods.sqlite3_next_stmt(db, stmt);
        }

        long ISQLite3Provider.sqlite3_last_insert_rowid(IntPtr db)
        {
            return NativeMethods.sqlite3_last_insert_rowid(db);
        }

        int ISQLite3Provider.sqlite3_changes(IntPtr db)
        {
            return NativeMethods.sqlite3_changes(db);
        }

        int ISQLite3Provider.sqlite3_total_changes(IntPtr db)
        {
            return NativeMethods.sqlite3_total_changes(db);
        }

        int ISQLite3Provider.sqlite3_extended_result_codes(IntPtr db, int onoff)
        {
            return NativeMethods.sqlite3_extended_result_codes(db, onoff);
        }

        string ISQLite3Provider.sqlite3_errstr(int rc)
        {
            return util.from_utf8(NativeMethods.sqlite3_errstr(rc));
        }

        int ISQLite3Provider.sqlite3_errcode(IntPtr db)
        {
            return NativeMethods.sqlite3_errcode(db);
        }

        int ISQLite3Provider.sqlite3_extended_errcode(IntPtr db)
        {
            return NativeMethods.sqlite3_extended_errcode(db);
        }

        int ISQLite3Provider.sqlite3_busy_timeout(IntPtr db, int ms)
        {
            return NativeMethods.sqlite3_busy_timeout(db, ms);
        }

        int ISQLite3Provider.sqlite3_get_autocommit(IntPtr db)
        {
            return NativeMethods.sqlite3_get_autocommit(db);
        }

        int ISQLite3Provider.sqlite3_db_readonly(IntPtr db, string dbName)
        {
            return NativeMethods.sqlite3_db_readonly(db, util.to_utf8(dbName)); 
        }
        
        string ISQLite3Provider.sqlite3_db_filename(IntPtr db, string att)
	{
            return util.from_utf8(NativeMethods.sqlite3_db_filename(db, util.to_utf8(att)));
	}

        string ISQLite3Provider.sqlite3_errmsg(IntPtr db)
        {
            return util.from_utf8(NativeMethods.sqlite3_errmsg(db));
        }

        string ISQLite3Provider.sqlite3_libversion()
        {
            return util.from_utf8(NativeMethods.sqlite3_libversion());
        }

        int ISQLite3Provider.sqlite3_libversion_number()
        {
            return NativeMethods.sqlite3_libversion_number();
        }

        int ISQLite3Provider.sqlite3_threadsafe()
        {
            return NativeMethods.sqlite3_threadsafe();
        }

        int ISQLite3Provider.sqlite3_config(int op)
        {
            return NativeMethods.sqlite3_config_none(op);
        }

        int ISQLite3Provider.sqlite3_config(int op, int val)
        {
            return NativeMethods.sqlite3_config_int(op, val);
        }

        int ISQLite3Provider.sqlite3_initialize()
        {
            return NativeMethods.sqlite3_initialize();
        }

        int ISQLite3Provider.sqlite3_shutdown()
        {
            return NativeMethods.sqlite3_shutdown();
        }

        int ISQLite3Provider.sqlite3_enable_load_extension(IntPtr db, int onoff)
        {
            return NativeMethods.sqlite3_enable_load_extension(db, onoff);
        }

        // ----------------------------------------------------------------

        // Passing a callback into SQLite is tricky.  The implementation details
        // can vary depending on the .NET implementation, so we hide these
        // in platform-specific code underneath the ISQLite3Provider boundary.
        //
        // The caller gives us a delegate and an object they want passed to that
        // delegate.  We do not actually pass that stuff down to SQLite as
        // the callback.  Instead, we store the information and pass down a bridge
        // function, with an IntPtr that can be used to retrieve the info later.
        //
        // When SQLite calls the bridge function, we lookup the info we previously
        // stored and call the delegate provided by the upper layer.
        //
        // The class we use to remember the original info (delegate and user object)
        // is shared but not portable.  It is in the util.cs file which is compiled
        // into each platform assembly.
        
        [MonoPInvokeCallback (typeof(NativeMethods.callback_commit))]
        static int commit_hook_bridge_impl(IntPtr p)
        {
            commit_hook_info hi = commit_hook_info.from_ptr(p);
            return hi.call();
        }

	NativeMethods.callback_commit commit_hook_bridge = new NativeMethods.callback_commit(commit_hook_bridge_impl); 
        void ISQLite3Provider.sqlite3_commit_hook(IntPtr db, delegate_commit func, object v)
        {
		var info = hooks.getOrCreateFor(db);
            if (info.commit != null)
            {
                // TODO maybe turn off the hook here, for now
                info.commit.free();
                info.commit = null;
            }

            if (func != null)
            {
                info.commit = new commit_hook_info(func, v);
                NativeMethods.sqlite3_commit_hook(db, commit_hook_bridge, info.commit.ptr);
            }
            else
            {
                NativeMethods.sqlite3_commit_hook(db, null, IntPtr.Zero);
            }
        }

        // ----------------------------------------------------------------

        // Passing a callback into SQLite is tricky.  See comments near commit_hook
        // implementation in pinvoke/SQLite3Provider.cs

        [MonoPInvokeCallback (typeof(NativeMethods.callback_scalar_function))]
        static void scalar_function_hook_bridge_impl(IntPtr context, int num_args, IntPtr argsptr)
        {
            IntPtr p = NativeMethods.sqlite3_user_data(context);
            scalar_function_hook_info hi = scalar_function_hook_info.from_ptr(p);
            hi.call(context, num_args, argsptr);
        }

	NativeMethods.callback_scalar_function scalar_function_hook_bridge = new NativeMethods.callback_scalar_function(scalar_function_hook_bridge_impl); 

        int my_sqlite3_create_function(IntPtr db, string name, int nargs, int flags, object v, delegate_function_scalar func)
        {
        // the keys for this dictionary are nargs.name, not just the name
            string key = string.Format("{0}.{1}", nargs, name);
		var info = hooks.getOrCreateFor(db);
            if (info.scalar.ContainsKey(key))
            {
                scalar_function_hook_info hi = info.scalar[key];

                // TODO maybe turn off the hook here, for now
                hi.free();

                info.scalar.Remove(key);
            }

            // 1 is SQLITE_UTF8
			int arg4 = 1 | flags;
            if (func != null)
            {
                scalar_function_hook_info hi = new scalar_function_hook_info(func, v);
                int rc = NativeMethods.sqlite3_create_function_v2(db, util.to_utf8(name), nargs, arg4, hi.ptr, scalar_function_hook_bridge, null, null, null);
                if (rc == 0)
                {
                    info.scalar[key] = hi;
                }
                return rc;
            }
            else
            {
                return NativeMethods.sqlite3_create_function_v2(db, util.to_utf8(name), nargs, arg4, IntPtr.Zero, null, null, null, null);
            }
        }

        int ISQLite3Provider.sqlite3_create_function(IntPtr db, string name, int nargs, object v, delegate_function_scalar func)
		{
			return my_sqlite3_create_function(db, name, nargs, 0, v, func);
		}

        int ISQLite3Provider.sqlite3_create_function(IntPtr db, string name, int nargs, int flags, object v, delegate_function_scalar func)
		{
			return my_sqlite3_create_function(db, name, nargs, flags, v, func);
		}

        // ----------------------------------------------------------------

        [MonoPInvokeCallback (typeof(NativeMethods.callback_log))]
        static void log_hook_bridge_impl(IntPtr p, int rc, IntPtr s)
        {
            log_hook_info hi = log_hook_info.from_ptr(p);
            hi.call(rc, util.from_utf8(s));
        }

	NativeMethods.callback_log log_hook_bridge = new NativeMethods.callback_log(log_hook_bridge_impl); 
        int ISQLite3Provider.sqlite3_config_log(delegate_log func, object v)
        {
            if (hooks.log != null)
            {
                // TODO maybe turn off the hook here, for now
                hooks.log.free();
                hooks.log = null;
            }

            if (func != null)
            {
                hooks.log = new log_hook_info(func, v);
                return NativeMethods.sqlite3_config_log(raw.SQLITE_CONFIG_LOG, log_hook_bridge, hooks.log.ptr);
            }
            else
            {
                return NativeMethods.sqlite3_config_log(raw.SQLITE_CONFIG_LOG, null, IntPtr.Zero);
            }
        }

        // ----------------------------------------------------------------

        // Passing a callback into SQLite is tricky.  See comments near commit_hook
        // implementation in pinvoke/SQLite3Provider.cs

        [MonoPInvokeCallback (typeof(NativeMethods.callback_agg_function_step))]
        static void agg_function_hook_bridge_step_impl(IntPtr context, int num_args, IntPtr argsptr)
        {
            IntPtr agg = NativeMethods.sqlite3_aggregate_context(context, 8);
            // TODO error check agg nomem

            IntPtr p = NativeMethods.sqlite3_user_data(context);
            agg_function_hook_info hi = agg_function_hook_info.from_ptr(p);
            hi.call_step(context, agg, num_args, argsptr);
        }

        [MonoPInvokeCallback (typeof(NativeMethods.callback_agg_function_final))]
        static void agg_function_hook_bridge_final_impl(IntPtr context)
        {
            IntPtr agg = NativeMethods.sqlite3_aggregate_context(context, 8);
            // TODO error check agg nomem

            IntPtr p = NativeMethods.sqlite3_user_data(context);
            agg_function_hook_info hi = agg_function_hook_info.from_ptr(p);
            hi.call_final(context, agg);
        }

	NativeMethods.callback_agg_function_step agg_function_hook_bridge_step = new NativeMethods.callback_agg_function_step(agg_function_hook_bridge_step_impl); 
	NativeMethods.callback_agg_function_final agg_function_hook_bridge_final = new NativeMethods.callback_agg_function_final(agg_function_hook_bridge_final_impl); 

        int my_sqlite3_create_function(IntPtr db, string name, int nargs, int flags, object v, delegate_function_aggregate_step func_step, delegate_function_aggregate_final func_final)
        {
        // the keys for this dictionary are nargs.name, not just the name
            string key = string.Format("{0}.{1}", nargs, name);
		var info = hooks.getOrCreateFor(db);
            if (info.agg.ContainsKey(key))
            {
                agg_function_hook_info hi = info.agg[key];

                // TODO maybe turn off the hook here, for now
                hi.free();

                info.agg.Remove(key);
            }

            // 1 is SQLITE_UTF8
			int arg4 = 1 | flags;
            if (func_step != null)
            {
                // TODO both func_step and func_final must be non-null
                agg_function_hook_info hi = new agg_function_hook_info(func_step, func_final, v);
                int rc = NativeMethods.sqlite3_create_function_v2(db, util.to_utf8(name), nargs, arg4, hi.ptr, null, agg_function_hook_bridge_step, agg_function_hook_bridge_final, null);
                if (rc == 0)
                {
                    info.agg[key] = hi;
                }
                return rc;
            }
            else
            {
                return NativeMethods.sqlite3_create_function_v2(db, util.to_utf8(name), nargs, arg4, IntPtr.Zero, null, null, null, null);
            }
        }

        int ISQLite3Provider.sqlite3_create_function(IntPtr db, string name, int nargs, object v, delegate_function_aggregate_step func_step, delegate_function_aggregate_final func_final)
		{
			return my_sqlite3_create_function(db, name, nargs, 0, v, func_step, func_final);
		}

        int ISQLite3Provider.sqlite3_create_function(IntPtr db, string name, int nargs, int flags, object v, delegate_function_aggregate_step func_step, delegate_function_aggregate_final func_final)
		{
			return my_sqlite3_create_function(db, name, nargs, flags, v, func_step, func_final);
		}

        // ----------------------------------------------------------------

        // Passing a callback into SQLite is tricky.  See comments near commit_hook
        // implementation in pinvoke/SQLite3Provider.cs

        static int collation_hook_bridge_impl(IntPtr p, int len1, IntPtr pv1, int len2, IntPtr pv2)
        {
            collation_hook_info hi = collation_hook_info.from_ptr(p);
            return hi.call(util.from_utf8(pv1, len1), util.from_utf8(pv2, len2));
        }

	NativeMethods.callback_collation collation_hook_bridge = new NativeMethods.callback_collation(collation_hook_bridge_impl); 
        int ISQLite3Provider.sqlite3_create_collation(IntPtr db, string name, object v, delegate_collation func)
        {
		var info = hooks.getOrCreateFor(db);
            if (info.collation.ContainsKey(name))
            {
                collation_hook_info hi = info.collation[name];

                // TODO maybe turn off the hook here, for now
                hi.free();

                info.collation.Remove(name);
            }

            // 1 is SQLITE_UTF8
            if (func != null)
            {
                collation_hook_info hi = new collation_hook_info(func, v);
                int rc = NativeMethods.sqlite3_create_collation(db, util.to_utf8(name), 1, hi.ptr, collation_hook_bridge);
                if (rc == 0)
                {
                    info.collation[name] = hi;
                }
                return rc;
            }
            else
            {
                return NativeMethods.sqlite3_create_collation(db, util.to_utf8(name), 1, IntPtr.Zero, null);
            }
        }

        // ----------------------------------------------------------------

        // Passing a callback into SQLite is tricky.  See comments near commit_hook
        // implementation in pinvoke/SQLite3Provider.cs

        [MonoPInvokeCallback (typeof(NativeMethods.callback_update))]
        static void update_hook_bridge_impl(IntPtr p, int typ, IntPtr db, IntPtr tbl, Int64 rowid)
        {
            update_hook_info hi = update_hook_info.from_ptr(p);
            hi.call(typ, util.from_utf8(db), util.from_utf8(tbl), rowid);
        }

	NativeMethods.callback_update update_hook_bridge = new NativeMethods.callback_update(update_hook_bridge_impl); 
        void ISQLite3Provider.sqlite3_update_hook(IntPtr db, delegate_update func, object v)
        {
		var info = hooks.getOrCreateFor(db);
            if (info.update != null)
            {
                // TODO maybe turn off the hook here, for now
                info.update.free();
                info.update = null;
            }

            if (func != null)
            {
                info.update = new update_hook_info(func, v);
                NativeMethods.sqlite3_update_hook(db, update_hook_bridge, info.update.ptr);
            }
            else
            {
                NativeMethods.sqlite3_update_hook(db, null, IntPtr.Zero);
            }
        }

        // ----------------------------------------------------------------

        // Passing a callback into SQLite is tricky.  See comments near commit_hook
        // implementation in pinvoke/SQLite3Provider.cs

        [MonoPInvokeCallback (typeof(NativeMethods.callback_rollback))]
        static void rollback_hook_bridge_impl(IntPtr p)
        {
            rollback_hook_info hi = rollback_hook_info.from_ptr(p);
            hi.call();
        }

	NativeMethods.callback_rollback rollback_hook_bridge = new NativeMethods.callback_rollback(rollback_hook_bridge_impl); 
        void ISQLite3Provider.sqlite3_rollback_hook(IntPtr db, delegate_rollback func, object v)
        {
		var info = hooks.getOrCreateFor(db);
            if (info.rollback != null)
            {
                // TODO maybe turn off the hook here, for now
                info.rollback.free();
                info.rollback = null;
            }

            if (func != null)
            {
                info.rollback = new rollback_hook_info(func, v);
                NativeMethods.sqlite3_rollback_hook(db, rollback_hook_bridge, info.rollback.ptr);
            }
            else
            {
                NativeMethods.sqlite3_rollback_hook(db, null, IntPtr.Zero);
            }
        }

        // ----------------------------------------------------------------

        // Passing a callback into SQLite is tricky.  See comments near commit_hook
        // implementation in pinvoke/SQLite3Provider.cs

        [MonoPInvokeCallback (typeof(NativeMethods.callback_trace))]
        static void trace_hook_bridge_impl(IntPtr p, IntPtr s)
        {
            trace_hook_info hi = trace_hook_info.from_ptr(p);
            hi.call(util.from_utf8(s));
        }

	NativeMethods.callback_trace trace_hook_bridge = new NativeMethods.callback_trace(trace_hook_bridge_impl); 
        void ISQLite3Provider.sqlite3_trace(IntPtr db, delegate_trace func, object v)
        {
		var info = hooks.getOrCreateFor(db);
            if (info.trace != null)
            {
                // TODO maybe turn off the hook here, for now
                info.trace.free();
                info.trace = null;
            }

            if (func != null)
            {
                info.trace = new trace_hook_info(func, v);
                NativeMethods.sqlite3_trace(db, trace_hook_bridge, info.trace.ptr);
            }
            else
            {
                NativeMethods.sqlite3_trace(db, null, IntPtr.Zero);
            }
        }

        // ----------------------------------------------------------------

        // Passing a callback into SQLite is tricky.  See comments near commit_hook
        // implementation in pinvoke/SQLite3Provider.cs

        [MonoPInvokeCallback (typeof(NativeMethods.callback_profile))]
        static void profile_hook_bridge_impl(IntPtr p, IntPtr s, long elapsed)
        {
            profile_hook_info hi = profile_hook_info.from_ptr(p);
            hi.call(util.from_utf8(s), elapsed);
        }

	NativeMethods.callback_profile profile_hook_bridge = new NativeMethods.callback_profile(profile_hook_bridge_impl); 
        void ISQLite3Provider.sqlite3_profile(IntPtr db, delegate_profile func, object v)
        {
		var info = hooks.getOrCreateFor(db);
            if (info.profile != null)
            {
                // TODO maybe turn off the hook here, for now
                info.profile.free();
                info.profile = null;
            }

            if (func != null)
            {
                info.profile = new profile_hook_info(func, v);
                NativeMethods.sqlite3_profile(db, profile_hook_bridge, info.profile.ptr);
            }
            else
            {
                NativeMethods.sqlite3_profile(db, null, IntPtr.Zero);
            }
        }

        // ----------------------------------------------------------------

        // Passing a callback into SQLite is tricky.  See comments near commit_hook
        // implementation in pinvoke/SQLite3Provider.cs

        [MonoPInvokeCallback (typeof(NativeMethods.callback_progress_handler))]
        static int progress_handler_hook_bridge_impl(IntPtr p)
        {
            progress_handler_hook_info hi = progress_handler_hook_info.from_ptr(p);
            return hi.call();
        }

        NativeMethods.callback_progress_handler progress_handler_hook_bridge = new NativeMethods.callback_progress_handler(progress_handler_hook_bridge_impl);
        void ISQLite3Provider.sqlite3_progress_handler(IntPtr db, int instructions, delegate_progress_handler func, object v)
        {
		var info = hooks.getOrCreateFor(db);
            if (info.progress != null)
            {
                // TODO maybe turn off the hook here, for now
                info.progress.free();
                info.progress = null;
            }

            if (func != null)
            {
                info.progress = new progress_handler_hook_info(func, v);
                NativeMethods.sqlite3_progress_handler(db, instructions, progress_handler_hook_bridge, info.progress.ptr);
            }
            else
            {
                NativeMethods.sqlite3_progress_handler(db, instructions, null, IntPtr.Zero);
            }
        }

        // ----------------------------------------------------------------

        // ----------------------------------------------------------------

        // Passing a callback into SQLite is tricky.  See comments near commit_hook
        // implementation in pinvoke/SQLite3Provider.cs

        [MonoPInvokeCallback (typeof(NativeMethods.callback_authorizer))]
        static int authorizer_hook_bridge_impl(IntPtr p, int action_code, IntPtr param0, IntPtr param1, IntPtr dbName, IntPtr inner_most_trigger_or_view)
        {
            authorizer_hook_info hi = authorizer_hook_info.from_ptr(p);
            return hi.call(action_code, util.from_utf8(param0), util.from_utf8(param1), util.from_utf8(dbName), util.from_utf8(inner_most_trigger_or_view));
        }

        NativeMethods.callback_authorizer authorizer_hook_bridge = new NativeMethods.callback_authorizer(authorizer_hook_bridge_impl);
        int ISQLite3Provider.sqlite3_set_authorizer(IntPtr db, delegate_authorizer func, object v)
        {
		var info = hooks.getOrCreateFor(db);
            if (info.authorizer != null)
            {
                // TODO maybe turn off the hook here, for now
                info.authorizer.free();
                info.authorizer = null;
            }

            if (func != null)
            {
                info.authorizer = new authorizer_hook_info(func, v);
                return NativeMethods.sqlite3_set_authorizer(db, authorizer_hook_bridge, info.authorizer.ptr);
            }
            else
            {
                return NativeMethods.sqlite3_set_authorizer(db, null, IntPtr.Zero);
            }
        }

        // ----------------------------------------------------------------

        long ISQLite3Provider.sqlite3_memory_used()
        {
            return NativeMethods.sqlite3_memory_used();
        }

        long ISQLite3Provider.sqlite3_memory_highwater(int resetFlag)
        {
            return NativeMethods.sqlite3_memory_highwater(resetFlag);
        }

        int ISQLite3Provider.sqlite3_status(int op, out int current, out int highwater, int resetFlag)
        {
            return NativeMethods.sqlite3_status(op, out current, out highwater, resetFlag);
        }

        string ISQLite3Provider.sqlite3_sourceid()
        {
            return util.from_utf8(NativeMethods.sqlite3_sourceid());
        }

        void ISQLite3Provider.sqlite3_result_int64(IntPtr ctx, long val)
        {
            NativeMethods.sqlite3_result_int64(ctx, val);
        }

        void ISQLite3Provider.sqlite3_result_int(IntPtr ctx, int val)
        {
            NativeMethods.sqlite3_result_int(ctx, val);
        }

        void ISQLite3Provider.sqlite3_result_double(IntPtr ctx, double val)
        {
            NativeMethods.sqlite3_result_double(ctx, val);
        }

        void ISQLite3Provider.sqlite3_result_null(IntPtr stm)
        {
            NativeMethods.sqlite3_result_null(stm);
        }

        void ISQLite3Provider.sqlite3_result_error(IntPtr ctx, string val)
        {
            NativeMethods.sqlite3_result_error(ctx, util.to_utf8(val), -1);
        }

        void ISQLite3Provider.sqlite3_result_text(IntPtr ctx, string val)
        {
            NativeMethods.sqlite3_result_text(ctx, util.to_utf8(val), -1, new IntPtr(-1));
        }

        void ISQLite3Provider.sqlite3_result_blob(IntPtr ctx, byte[] blob)
        {
            NativeMethods.sqlite3_result_blob(ctx, blob, blob.Length, new IntPtr(-1));
        }

        void ISQLite3Provider.sqlite3_result_zeroblob(IntPtr ctx, int n)
        {
            NativeMethods.sqlite3_result_zeroblob(ctx, n);
        }

        // TODO sqlite3_result_value

        void ISQLite3Provider.sqlite3_result_error_toobig(IntPtr ctx)
        {
            NativeMethods.sqlite3_result_error_toobig(ctx);
        }

        void ISQLite3Provider.sqlite3_result_error_nomem(IntPtr ctx)
        {
            NativeMethods.sqlite3_result_error_nomem(ctx);
        }

        void ISQLite3Provider.sqlite3_result_error_code(IntPtr ctx, int code)
        {
            NativeMethods.sqlite3_result_error_code(ctx, code);
        }

        byte[] ISQLite3Provider.sqlite3_value_blob(IntPtr p)
        {
            IntPtr blobPointer = NativeMethods.sqlite3_value_blob(p);
            if (blobPointer == IntPtr.Zero)
            {
                return null;
            }

            var length = NativeMethods.sqlite3_value_bytes(p);
            byte[] result = new byte[length];
            Marshal.Copy(blobPointer, (byte[])result, 0, length);
            return result;
        }

        int ISQLite3Provider.sqlite3_value_bytes(IntPtr p)
        {
            return NativeMethods.sqlite3_value_bytes(p);
        }

        double ISQLite3Provider.sqlite3_value_double(IntPtr p)
        {
            return NativeMethods.sqlite3_value_double(p);
        }

        int ISQLite3Provider.sqlite3_value_int(IntPtr p)
        {
            return NativeMethods.sqlite3_value_int(p);
        }

        long ISQLite3Provider.sqlite3_value_int64(IntPtr p)
        {
            return NativeMethods.sqlite3_value_int64(p);
        }

        int ISQLite3Provider.sqlite3_value_type(IntPtr p)
        {
            return NativeMethods.sqlite3_value_type(p);
        }

        string ISQLite3Provider.sqlite3_value_text(IntPtr p)
        {
            return util.from_utf8(NativeMethods.sqlite3_value_text(p));
        }

        int ISQLite3Provider.sqlite3_bind_int(IntPtr stm, int paramIndex, int val)
        {
            return NativeMethods.sqlite3_bind_int(stm, paramIndex, val);
        }

        int ISQLite3Provider.sqlite3_bind_int64(IntPtr stm, int paramIndex, long val)
        {
            return NativeMethods.sqlite3_bind_int64(stm, paramIndex, val);
        }

        int ISQLite3Provider.sqlite3_bind_text(IntPtr stm, int paramIndex, string t)
        {
            return NativeMethods.sqlite3_bind_text(stm, paramIndex, util.to_utf8(t), -1, new IntPtr(-1));
        }

        int ISQLite3Provider.sqlite3_bind_double(IntPtr stm, int paramIndex, double val)
        {
            return NativeMethods.sqlite3_bind_double(stm, paramIndex, val);
        }

        int ISQLite3Provider.sqlite3_bind_blob(IntPtr stm, int paramIndex, byte[] blob)
        {
            return NativeMethods.sqlite3_bind_blob(stm, paramIndex, blob, blob.Length, new IntPtr(-1));
        }

        int ISQLite3Provider.sqlite3_bind_blob(IntPtr stm, int paramIndex, byte[] blob, int nSize)
        {
            return NativeMethods.sqlite3_bind_blob(stm, paramIndex, blob, nSize, new IntPtr(-1));
        }

        int ISQLite3Provider.sqlite3_bind_zeroblob(IntPtr stm, int paramIndex, int size)
        {
            return NativeMethods.sqlite3_bind_zeroblob(stm, paramIndex, size);
        }

        int ISQLite3Provider.sqlite3_bind_null(IntPtr stm, int paramIndex)
        {
            return NativeMethods.sqlite3_bind_null(stm, paramIndex);
        }

        int ISQLite3Provider.sqlite3_bind_parameter_count(IntPtr stm)
        {
            return NativeMethods.sqlite3_bind_parameter_count(stm);
        }

        string ISQLite3Provider.sqlite3_bind_parameter_name(IntPtr stm, int paramIndex)
        {
            return util.from_utf8(NativeMethods.sqlite3_bind_parameter_name(stm, paramIndex));
        }

        int ISQLite3Provider.sqlite3_bind_parameter_index(IntPtr stm, string paramName)
        {
            return NativeMethods.sqlite3_bind_parameter_index(stm, util.to_utf8(paramName));
        }

        int ISQLite3Provider.sqlite3_step(IntPtr stm)
        {
            return NativeMethods.sqlite3_step(stm);
        }

        int ISQLite3Provider.sqlite3_stmt_busy(IntPtr stm)
        {
            return NativeMethods.sqlite3_stmt_busy(stm);
        }

        int ISQLite3Provider.sqlite3_stmt_readonly(IntPtr stm)
        {
            return NativeMethods.sqlite3_stmt_readonly(stm);
        }

        int ISQLite3Provider.sqlite3_column_int(IntPtr stm, int columnIndex)
        {
            return NativeMethods.sqlite3_column_int(stm, columnIndex);
        }

        long ISQLite3Provider.sqlite3_column_int64(IntPtr stm, int columnIndex)
        {
            return NativeMethods.sqlite3_column_int64(stm, columnIndex);
        }

        string ISQLite3Provider.sqlite3_column_text(IntPtr stm, int columnIndex)
        {
            return util.from_utf8(NativeMethods.sqlite3_column_text(stm, columnIndex));
        }

        string ISQLite3Provider.sqlite3_column_decltype(IntPtr stm, int columnIndex)
        {
            return util.from_utf8(NativeMethods.sqlite3_column_decltype(stm, columnIndex));
        }

        double ISQLite3Provider.sqlite3_column_double(IntPtr stm, int columnIndex)
        {
            return NativeMethods.sqlite3_column_double(stm, columnIndex);
        }

        byte[] ISQLite3Provider.sqlite3_column_blob(IntPtr stm, int columnIndex)
        {
            IntPtr blobPointer = NativeMethods.sqlite3_column_blob(stm, columnIndex);
            if (blobPointer == IntPtr.Zero)
            {
                return null;
            }

            var length = NativeMethods.sqlite3_column_bytes(stm, columnIndex);
            byte[] result = new byte[length];
            Marshal.Copy(blobPointer, (byte[])result, 0, length);
            return result;
        }

        int ISQLite3Provider.sqlite3_column_blob(IntPtr stm, int columnIndex, byte[] result, int offset)
        {
            if (result == null || offset >= result.Length)
            {
                return raw.SQLITE_ERROR;
            }
            IntPtr blobPointer = NativeMethods.sqlite3_column_blob(stm, columnIndex);
            if (blobPointer == IntPtr.Zero)
            {
                return raw.SQLITE_ERROR;
            }

            var length = NativeMethods.sqlite3_column_bytes(stm, columnIndex);
            if (offset + length > result.Length)
            {
                return raw.SQLITE_ERROR;
            }
            Marshal.Copy(blobPointer, (byte[])result, offset, length);
            return raw.SQLITE_OK;
        }

        int ISQLite3Provider.sqlite3_column_type(IntPtr stm, int columnIndex)
        {
            return NativeMethods.sqlite3_column_type(stm, columnIndex);
        }

        int ISQLite3Provider.sqlite3_column_bytes(IntPtr stm, int columnIndex)
        {
            return NativeMethods.sqlite3_column_bytes(stm, columnIndex);
        }

        int ISQLite3Provider.sqlite3_column_count(IntPtr stm)
        {
            return NativeMethods.sqlite3_column_count(stm);
        }

        int ISQLite3Provider.sqlite3_data_count(IntPtr stm)
        {
            return NativeMethods.sqlite3_data_count(stm);
        }

        string ISQLite3Provider.sqlite3_column_name(IntPtr stm, int columnIndex)
        {
            return util.from_utf8(NativeMethods.sqlite3_column_name(stm, columnIndex));
        }

        string ISQLite3Provider.sqlite3_column_origin_name(IntPtr stm, int columnIndex)
        {
            return util.from_utf8(NativeMethods.sqlite3_column_origin_name(stm, columnIndex));
        }

        string ISQLite3Provider.sqlite3_column_table_name(IntPtr stm, int columnIndex)
        {
            return util.from_utf8(NativeMethods.sqlite3_column_table_name(stm, columnIndex));
        }

        string ISQLite3Provider.sqlite3_column_database_name(IntPtr stm, int columnIndex)
        {
            return util.from_utf8(NativeMethods.sqlite3_column_database_name(stm, columnIndex));
        }

        int ISQLite3Provider.sqlite3_reset(IntPtr stm)
        {
            return NativeMethods.sqlite3_reset(stm);
        }

        int ISQLite3Provider.sqlite3_clear_bindings(IntPtr stm)
        {
            return NativeMethods.sqlite3_clear_bindings(stm);
        }

        int ISQLite3Provider.sqlite3_stmt_status(IntPtr stm, int op, int resetFlg)
        {
            return NativeMethods.sqlite3_stmt_status(stm, op, resetFlg);
        }

        int ISQLite3Provider.sqlite3_finalize(IntPtr stm)
        {
            return NativeMethods.sqlite3_finalize(stm);
        }

        int ISQLite3Provider.sqlite3_wal_autocheckpoint(IntPtr db, int n)
        {
            return NativeMethods.sqlite3_wal_autocheckpoint(db, n);
        }

        int ISQLite3Provider.sqlite3_wal_checkpoint(IntPtr db, string dbName)
        {
            return NativeMethods.sqlite3_wal_checkpoint(db, util.to_utf8(dbName));
        }

        int ISQLite3Provider.sqlite3_wal_checkpoint_v2(IntPtr db, string dbName, int eMode, out int logSize, out int framesCheckPointed)
        {
            return NativeMethods.sqlite3_wal_checkpoint_v2(db, util.to_utf8(dbName), eMode, out logSize, out framesCheckPointed);
        }

	static class NativeMethods
	{
		static void Load<T>(IGetFunctionPtr gf, out T del)
			where T : class
		{
			var delegate_type = typeof(T);
			// TODO check here to make sure the type is a delegate of some kind?
			// just in case we introduce other properties later?
			var name = delegate_type.Name;
			foreach (var attr in delegate_type.GetTypeInfo().GetCustomAttributes())
			{
				if (attr.GetType() == typeof(EntryPointAttribute))
				{
					var ep = attr as EntryPointAttribute;
					//System.Console.WriteLine("{0} EntryPoint {1}", name, ep.Name);
					name = ep.Name;
				}
			}
			var fn_ptr = gf.GetFunctionPtr(name);
			if (fn_ptr != IntPtr.Zero)
			{
				var d = Marshal.GetDelegateForFunctionPointer<T>(fn_ptr);
				del = d;
			}
			else
			{
				//System.Console.WriteLine("Warning: {0} not found", name);
				del = null;
			}
		}

		static public void Setup(IGetFunctionPtr gf)
		{
			Load(gf, out sqlite3_close);
			Load(gf, out sqlite3_close_v2);
			Load(gf, out sqlite3_enable_shared_cache);
			Load(gf, out sqlite3_interrupt);
			Load(gf, out sqlite3_finalize);
			Load(gf, out sqlite3_reset);
			Load(gf, out sqlite3_clear_bindings);
			Load(gf, out sqlite3_stmt_status);
			Load(gf, out sqlite3_bind_parameter_name);
			Load(gf, out sqlite3_column_database_name);
			Load(gf, out sqlite3_column_decltype);
			Load(gf, out sqlite3_column_name);
			Load(gf, out sqlite3_column_origin_name);
			Load(gf, out sqlite3_column_table_name);
			Load(gf, out sqlite3_column_text);
			Load(gf, out sqlite3_errmsg);
			Load(gf, out sqlite3_db_readonly);
			Load(gf, out sqlite3_db_filename);
			Load(gf, out sqlite3_prepare);
			Load(gf, out sqlite3_prepare_v2);
			Load(gf, out sqlite3_db_status);
			Load(gf, out sqlite3_complete);
			Load(gf, out sqlite3_compileoption_used);
			Load(gf, out sqlite3_compileoption_get);
			Load(gf, out sqlite3_table_column_metadata);
			Load(gf, out sqlite3_value_text);
			Load(gf, out sqlite3_enable_load_extension);
			Load(gf, out sqlite3_load_extension);
			Load(gf, out sqlite3_initialize);
			Load(gf, out sqlite3_shutdown);
			Load(gf, out sqlite3_libversion);
			Load(gf, out sqlite3_libversion_number);
			Load(gf, out sqlite3_threadsafe);
			Load(gf, out sqlite3_sourceid);
			Load(gf, out sqlite3_malloc);
			Load(gf, out sqlite3_realloc);
			Load(gf, out sqlite3_free);
			Load(gf, out sqlite3_open);
			Load(gf, out sqlite3_open_v2);
			Load(gf, out sqlite3_vfs_find);
			Load(gf, out sqlite3_last_insert_rowid);
			Load(gf, out sqlite3_changes);
			Load(gf, out sqlite3_total_changes);
			Load(gf, out sqlite3_memory_used);
			Load(gf, out sqlite3_memory_highwater);
			Load(gf, out sqlite3_status);
			Load(gf, out sqlite3_busy_timeout);
			Load(gf, out sqlite3_bind_blob);
			Load(gf, out sqlite3_bind_zeroblob);
			Load(gf, out sqlite3_bind_double);
			Load(gf, out sqlite3_bind_int);
			Load(gf, out sqlite3_bind_int64);
			Load(gf, out sqlite3_bind_null);
			Load(gf, out sqlite3_bind_text);
			Load(gf, out sqlite3_bind_parameter_count);
			Load(gf, out sqlite3_bind_parameter_index);
			Load(gf, out sqlite3_column_count);
			Load(gf, out sqlite3_data_count);
			Load(gf, out sqlite3_step);
			Load(gf, out sqlite3_sql);
			Load(gf, out sqlite3_column_double);
			Load(gf, out sqlite3_column_int);
			Load(gf, out sqlite3_column_int64);
			Load(gf, out sqlite3_column_blob);
			Load(gf, out sqlite3_column_bytes);
			Load(gf, out sqlite3_column_type);
			Load(gf, out sqlite3_aggregate_count);
			Load(gf, out sqlite3_value_blob);
			Load(gf, out sqlite3_value_bytes);
			Load(gf, out sqlite3_value_double);
			Load(gf, out sqlite3_value_int);
			Load(gf, out sqlite3_value_int64);
			Load(gf, out sqlite3_value_type);
			Load(gf, out sqlite3_user_data);
			Load(gf, out sqlite3_result_blob);
			Load(gf, out sqlite3_result_double);
			Load(gf, out sqlite3_result_error);
			Load(gf, out sqlite3_result_int);
			Load(gf, out sqlite3_result_int64);
			Load(gf, out sqlite3_result_null);
			Load(gf, out sqlite3_result_text);
			Load(gf, out sqlite3_result_zeroblob);
			// TODO sqlite3_result_value 
			Load(gf, out sqlite3_result_error_toobig);
			Load(gf, out sqlite3_result_error_nomem);
			Load(gf, out sqlite3_result_error_code);
			Load(gf, out sqlite3_aggregate_context);
			Load(gf, out sqlite3_key);
			Load(gf, out sqlite3_rekey);
			Load(gf, out sqlite3_config_none);
			Load(gf, out sqlite3_config_int);
			Load(gf, out sqlite3_config_log);
			Load(gf, out sqlite3_create_function_v2);
			Load(gf, out sqlite3_create_collation);
			Load(gf, out sqlite3_update_hook);
			Load(gf, out sqlite3_commit_hook);
			Load(gf, out sqlite3_profile);
			Load(gf, out sqlite3_progress_handler);
			Load(gf, out sqlite3_trace);
			Load(gf, out sqlite3_rollback_hook);
			Load(gf, out sqlite3_db_handle);
			Load(gf, out sqlite3_next_stmt);
			Load(gf, out sqlite3_stmt_busy);
			Load(gf, out sqlite3_stmt_readonly);
			Load(gf, out sqlite3_exec);
			Load(gf, out sqlite3_get_autocommit);
			Load(gf, out sqlite3_extended_result_codes);
			Load(gf, out sqlite3_errcode);
			Load(gf, out sqlite3_extended_errcode);
			Load(gf, out sqlite3_errstr);
			Load(gf, out sqlite3_log);
			Load(gf, out sqlite3_file_control);
			Load(gf, out sqlite3_backup_init);
			Load(gf, out sqlite3_backup_step);
			Load(gf, out sqlite3_backup_finish);
			Load(gf, out sqlite3_backup_remaining);
			Load(gf, out sqlite3_backup_pagecount);
			Load(gf, out sqlite3_blob_open);
			Load(gf, out sqlite3_blob_write);
			Load(gf, out sqlite3_blob_read);
			Load(gf, out sqlite3_blob_bytes);
			Load(gf, out sqlite3_blob_close);
			Load(gf, out sqlite3_wal_autocheckpoint);
			Load(gf, out sqlite3_wal_checkpoint);
			Load(gf, out sqlite3_wal_checkpoint_v2);
			Load(gf, out sqlite3_set_authorizer);
			Load(gf, out sqlite3_win32_set_directory);
		}

		public static MyDelegateTypes.sqlite3_close sqlite3_close;
		public static MyDelegateTypes.sqlite3_close_v2 sqlite3_close_v2;
		public static MyDelegateTypes.sqlite3_enable_shared_cache sqlite3_enable_shared_cache;
		public static MyDelegateTypes.sqlite3_interrupt sqlite3_interrupt;
		public static MyDelegateTypes.sqlite3_finalize sqlite3_finalize;
		public static MyDelegateTypes.sqlite3_reset sqlite3_reset;
		public static MyDelegateTypes.sqlite3_clear_bindings sqlite3_clear_bindings;
		public static MyDelegateTypes.sqlite3_stmt_status sqlite3_stmt_status;
		public static MyDelegateTypes.sqlite3_bind_parameter_name sqlite3_bind_parameter_name;
		public static MyDelegateTypes.sqlite3_column_database_name sqlite3_column_database_name;
		public static MyDelegateTypes.sqlite3_column_decltype sqlite3_column_decltype;
		public static MyDelegateTypes.sqlite3_column_name sqlite3_column_name;
		public static MyDelegateTypes.sqlite3_column_origin_name sqlite3_column_origin_name;
		public static MyDelegateTypes.sqlite3_column_table_name sqlite3_column_table_name;
		public static MyDelegateTypes.sqlite3_column_text sqlite3_column_text;
		public static MyDelegateTypes.sqlite3_errmsg sqlite3_errmsg;
		public static MyDelegateTypes.sqlite3_db_readonly sqlite3_db_readonly;
		public static MyDelegateTypes.sqlite3_db_filename sqlite3_db_filename;
		public static MyDelegateTypes.sqlite3_prepare sqlite3_prepare;
		public static MyDelegateTypes.sqlite3_prepare_v2 sqlite3_prepare_v2;
		public static MyDelegateTypes.sqlite3_db_status sqlite3_db_status;
		public static MyDelegateTypes.sqlite3_complete sqlite3_complete;
		public static MyDelegateTypes.sqlite3_compileoption_used sqlite3_compileoption_used;
		public static MyDelegateTypes.sqlite3_compileoption_get sqlite3_compileoption_get;
		public static MyDelegateTypes.sqlite3_table_column_metadata sqlite3_table_column_metadata;
		public static MyDelegateTypes.sqlite3_value_text sqlite3_value_text;
		public static MyDelegateTypes.sqlite3_enable_load_extension sqlite3_enable_load_extension;
		public static MyDelegateTypes.sqlite3_load_extension sqlite3_load_extension;
		public static MyDelegateTypes.sqlite3_initialize sqlite3_initialize;
		public static MyDelegateTypes.sqlite3_shutdown sqlite3_shutdown;
		public static MyDelegateTypes.sqlite3_libversion sqlite3_libversion;
		public static MyDelegateTypes.sqlite3_libversion_number sqlite3_libversion_number;
		public static MyDelegateTypes.sqlite3_threadsafe sqlite3_threadsafe;
		public static MyDelegateTypes.sqlite3_sourceid sqlite3_sourceid;
		public static MyDelegateTypes.sqlite3_malloc sqlite3_malloc;
		public static MyDelegateTypes.sqlite3_realloc sqlite3_realloc;
		public static MyDelegateTypes.sqlite3_free sqlite3_free;
		public static MyDelegateTypes.sqlite3_open sqlite3_open;
		public static MyDelegateTypes.sqlite3_open_v2 sqlite3_open_v2;
		public static MyDelegateTypes.sqlite3_vfs_find sqlite3_vfs_find;
		public static MyDelegateTypes.sqlite3_last_insert_rowid sqlite3_last_insert_rowid;
		public static MyDelegateTypes.sqlite3_changes sqlite3_changes;
		public static MyDelegateTypes.sqlite3_total_changes sqlite3_total_changes;
		public static MyDelegateTypes.sqlite3_memory_used sqlite3_memory_used;
		public static MyDelegateTypes.sqlite3_memory_highwater sqlite3_memory_highwater;
		public static MyDelegateTypes.sqlite3_status sqlite3_status;
		public static MyDelegateTypes.sqlite3_busy_timeout sqlite3_busy_timeout;
		public static MyDelegateTypes.sqlite3_bind_blob sqlite3_bind_blob;
		public static MyDelegateTypes.sqlite3_bind_zeroblob sqlite3_bind_zeroblob;
		public static MyDelegateTypes.sqlite3_bind_double sqlite3_bind_double;
		public static MyDelegateTypes.sqlite3_bind_int sqlite3_bind_int;
		public static MyDelegateTypes.sqlite3_bind_int64 sqlite3_bind_int64;
		public static MyDelegateTypes.sqlite3_bind_null sqlite3_bind_null;
		public static MyDelegateTypes.sqlite3_bind_text sqlite3_bind_text;
		public static MyDelegateTypes.sqlite3_bind_parameter_count sqlite3_bind_parameter_count;
		public static MyDelegateTypes.sqlite3_bind_parameter_index sqlite3_bind_parameter_index;
		public static MyDelegateTypes.sqlite3_column_count sqlite3_column_count;
		public static MyDelegateTypes.sqlite3_data_count sqlite3_data_count;
		public static MyDelegateTypes.sqlite3_step sqlite3_step;
		public static MyDelegateTypes.sqlite3_sql sqlite3_sql;
		public static MyDelegateTypes.sqlite3_column_double sqlite3_column_double;
		public static MyDelegateTypes.sqlite3_column_int sqlite3_column_int;
		public static MyDelegateTypes.sqlite3_column_int64 sqlite3_column_int64;
		public static MyDelegateTypes.sqlite3_column_blob sqlite3_column_blob;
		public static MyDelegateTypes.sqlite3_column_bytes sqlite3_column_bytes;
		public static MyDelegateTypes.sqlite3_column_type sqlite3_column_type;
		public static MyDelegateTypes.sqlite3_aggregate_count sqlite3_aggregate_count;
		public static MyDelegateTypes.sqlite3_value_blob sqlite3_value_blob;
		public static MyDelegateTypes.sqlite3_value_bytes sqlite3_value_bytes;
		public static MyDelegateTypes.sqlite3_value_double sqlite3_value_double;
		public static MyDelegateTypes.sqlite3_value_int sqlite3_value_int;
		public static MyDelegateTypes.sqlite3_value_int64 sqlite3_value_int64;
		public static MyDelegateTypes.sqlite3_value_type sqlite3_value_type;
		public static MyDelegateTypes.sqlite3_user_data sqlite3_user_data;
		public static MyDelegateTypes.sqlite3_result_blob sqlite3_result_blob;
		public static MyDelegateTypes.sqlite3_result_double sqlite3_result_double;
		public static MyDelegateTypes.sqlite3_result_error sqlite3_result_error;
		public static MyDelegateTypes.sqlite3_result_int sqlite3_result_int;
		public static MyDelegateTypes.sqlite3_result_int64 sqlite3_result_int64;
		public static MyDelegateTypes.sqlite3_result_null sqlite3_result_null;
		public static MyDelegateTypes.sqlite3_result_text sqlite3_result_text;
		public static MyDelegateTypes.sqlite3_result_zeroblob sqlite3_result_zeroblob;
		// TODO sqlite3_result_value 
		public static MyDelegateTypes.sqlite3_result_error_toobig sqlite3_result_error_toobig;
		public static MyDelegateTypes.sqlite3_result_error_nomem sqlite3_result_error_nomem;
		public static MyDelegateTypes.sqlite3_result_error_code sqlite3_result_error_code;
		public static MyDelegateTypes.sqlite3_aggregate_context sqlite3_aggregate_context;
		public static MyDelegateTypes.sqlite3_key sqlite3_key;
		public static MyDelegateTypes.sqlite3_rekey sqlite3_rekey;
		public static MyDelegateTypes.sqlite3_config_none sqlite3_config_none;
		public static MyDelegateTypes.sqlite3_config_int sqlite3_config_int;
		public static MyDelegateTypes.sqlite3_config_log sqlite3_config_log;
		public static MyDelegateTypes.sqlite3_create_function_v2 sqlite3_create_function_v2;
		public static MyDelegateTypes.sqlite3_create_collation sqlite3_create_collation;
		public static MyDelegateTypes.sqlite3_update_hook sqlite3_update_hook;
		public static MyDelegateTypes.sqlite3_commit_hook sqlite3_commit_hook;
		public static MyDelegateTypes.sqlite3_profile sqlite3_profile;
		public static MyDelegateTypes.sqlite3_progress_handler sqlite3_progress_handler;
		public static MyDelegateTypes.sqlite3_trace sqlite3_trace;
		public static MyDelegateTypes.sqlite3_rollback_hook sqlite3_rollback_hook;
		public static MyDelegateTypes.sqlite3_db_handle sqlite3_db_handle;
		public static MyDelegateTypes.sqlite3_next_stmt sqlite3_next_stmt;
		public static MyDelegateTypes.sqlite3_stmt_busy sqlite3_stmt_busy;
		public static MyDelegateTypes.sqlite3_stmt_readonly sqlite3_stmt_readonly;
		public static MyDelegateTypes.sqlite3_exec sqlite3_exec;
		public static MyDelegateTypes.sqlite3_get_autocommit sqlite3_get_autocommit;
		public static MyDelegateTypes.sqlite3_extended_result_codes sqlite3_extended_result_codes;
		public static MyDelegateTypes.sqlite3_errcode sqlite3_errcode;
		public static MyDelegateTypes.sqlite3_extended_errcode sqlite3_extended_errcode;
		public static MyDelegateTypes.sqlite3_errstr sqlite3_errstr;
		public static MyDelegateTypes.sqlite3_log sqlite3_log;
		public static MyDelegateTypes.sqlite3_file_control sqlite3_file_control;
		public static MyDelegateTypes.sqlite3_backup_init sqlite3_backup_init;
		public static MyDelegateTypes.sqlite3_backup_step sqlite3_backup_step;
		public static MyDelegateTypes.sqlite3_backup_finish sqlite3_backup_finish;
		public static MyDelegateTypes.sqlite3_backup_remaining sqlite3_backup_remaining;
		public static MyDelegateTypes.sqlite3_backup_pagecount sqlite3_backup_pagecount;
		public static MyDelegateTypes.sqlite3_blob_open sqlite3_blob_open;
		public static MyDelegateTypes.sqlite3_blob_write sqlite3_blob_write;
		public static MyDelegateTypes.sqlite3_blob_read sqlite3_blob_read;
		public static MyDelegateTypes.sqlite3_blob_bytes sqlite3_blob_bytes;
		public static MyDelegateTypes.sqlite3_blob_close sqlite3_blob_close;
		public static MyDelegateTypes.sqlite3_wal_autocheckpoint sqlite3_wal_autocheckpoint;
		public static MyDelegateTypes.sqlite3_wal_checkpoint sqlite3_wal_checkpoint;
		public static MyDelegateTypes.sqlite3_wal_checkpoint_v2 sqlite3_wal_checkpoint_v2;
		public static MyDelegateTypes.sqlite3_set_authorizer sqlite3_set_authorizer;
		public static MyDelegateTypes.sqlite3_win32_set_directory sqlite3_win32_set_directory ;


	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate void callback_log(IntPtr pUserData, int errorCode, IntPtr pMessage);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate void callback_scalar_function(IntPtr context, int nArgs, IntPtr argsptr);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate void callback_agg_function_step(IntPtr context, int nArgs, IntPtr argsptr);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate void callback_agg_function_final(IntPtr context);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate void callback_destroy(IntPtr p);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate int callback_collation(IntPtr puser, int len1, IntPtr pv1, int len2, IntPtr pv2);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate void callback_update(IntPtr p, int typ, IntPtr db, IntPtr tbl, long rowid);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate int callback_commit(IntPtr puser);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate void callback_profile(IntPtr puser, IntPtr statement, long elapsed);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate int callback_progress_handler(IntPtr puser);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate int callback_authorizer(IntPtr puser, int action_code, IntPtr param0, IntPtr param1, IntPtr dbName, IntPtr inner_most_trigger_or_view);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate void callback_trace(IntPtr puser, IntPtr statement);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate void callback_rollback(IntPtr puser);

	[UnmanagedFunctionPointer(CALLING_CONVENTION)]
	public delegate int callback_exec(IntPtr db, int n, IntPtr values, IntPtr names);
	}

	static class MyDelegateTypes
	{
		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_close(IntPtr db);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_close_v2(IntPtr db);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_enable_shared_cache(int enable);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_interrupt(IntPtr db);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_finalize(IntPtr stmt);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_reset(IntPtr stmt);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_clear_bindings(IntPtr stmt);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_stmt_status(IntPtr stm, int op, int resetFlg);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_bind_parameter_name(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_column_database_name(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_column_decltype(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_column_name(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_column_origin_name(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_column_table_name(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_column_text(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_errmsg(IntPtr db);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_db_readonly(IntPtr db, byte[] dbName);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_db_filename(IntPtr db, byte[] att);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_prepare(IntPtr db, IntPtr pSql, int nBytes, out IntPtr stmt, out IntPtr ptrRemain);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_prepare_v2(IntPtr db, IntPtr pSql, int nBytes, out IntPtr stmt, out IntPtr ptrRemain);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_db_status(IntPtr db, int op, out int current, out int highest, int resetFlg);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_complete(byte[] pSql);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_compileoption_used(byte[] pSql);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_compileoption_get(int n);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_table_column_metadata(IntPtr db, byte[] dbName, byte[] tblName, byte[] colName, out IntPtr ptrDataType, out IntPtr ptrCollSeq, out int notNull, out int primaryKey, out int autoInc);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_value_text(IntPtr p);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_enable_load_extension(
		IntPtr db, int enable);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_load_extension(
		IntPtr db, byte[] fileName, byte[] procName, ref IntPtr pError);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_initialize();

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_shutdown();

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_libversion();

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_libversion_number();

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_threadsafe();

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_sourceid();

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_malloc(int n);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_realloc(IntPtr p, int n);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_free(IntPtr p);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_open(byte[] filename, out IntPtr db);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_open_v2(byte[] filename, out IntPtr db, int flags, byte[] vfs);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_vfs_find(byte[] vfs);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate long sqlite3_last_insert_rowid(IntPtr db);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_changes(IntPtr db);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_total_changes(IntPtr db);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate long sqlite3_memory_used();

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate long sqlite3_memory_highwater(int resetFlag);
		
		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_status(int op, out int current, out int highwater, int resetFlag);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_busy_timeout(IntPtr db, int ms);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_bind_blob(IntPtr stmt, int index, byte[] val, int nSize, IntPtr nTransient);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_bind_zeroblob(IntPtr stmt, int index, int size);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_bind_double(IntPtr stmt, int index, double val);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_bind_int(IntPtr stmt, int index, int val);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_bind_int64(IntPtr stmt, int index, long val);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_bind_null(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_bind_text(IntPtr stmt, int index, byte[] val, int nlen, IntPtr pvReserved);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_bind_parameter_count(IntPtr stmt);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_bind_parameter_index(IntPtr stmt, byte[] strName);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_column_count(IntPtr stmt);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_data_count(IntPtr stmt);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_step(IntPtr stmt);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_sql(IntPtr stmt);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate double sqlite3_column_double(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_column_int(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate long sqlite3_column_int64(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_column_blob(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_column_bytes(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_column_type(IntPtr stmt, int index);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_aggregate_count(IntPtr context);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_value_blob(IntPtr p);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_value_bytes(IntPtr p);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate double sqlite3_value_double(IntPtr p);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_value_int(IntPtr p);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate long sqlite3_value_int64(IntPtr p);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_value_type(IntPtr p);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_user_data(IntPtr context);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_result_blob(IntPtr context, byte[] val, int nSize, IntPtr pvReserved);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_result_double(IntPtr context, double val);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_result_error(IntPtr context, byte[] strErr, int nLen);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_result_int(IntPtr context, int val);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_result_int64(IntPtr context, long val);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_result_null(IntPtr context);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_result_text(IntPtr context, byte[] val, int nLen, IntPtr pvReserved);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_result_zeroblob(IntPtr context, int n);

		// TODO sqlite3_result_value 

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_result_error_toobig(IntPtr context);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_result_error_nomem(IntPtr context);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_result_error_code(IntPtr context, int code);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_aggregate_context(IntPtr context, int nBytes);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_key(IntPtr db, byte[] key, int keylen);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_rekey(IntPtr db, byte[] key, int keylen);

		// Since sqlite3_config() takes a variable argument list, we have to overload declarations
		// for all possible calls that we want to use.
		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		[EntryPoint("sqlite3_config")]
		public delegate int sqlite3_config_none(int op);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		[EntryPoint("sqlite3_config")]
		public delegate int sqlite3_config_int(int op, int val);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		[EntryPoint("sqlite3_config")]
		public delegate int sqlite3_config_log(int op, NativeMethods.callback_log func, IntPtr pvUser);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_create_collation(IntPtr db, byte[] strName, int nType, IntPtr pvUser, NativeMethods.callback_collation func);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_update_hook(IntPtr db, NativeMethods.callback_update func, IntPtr pvUser);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_commit_hook(IntPtr db, NativeMethods.callback_commit func, IntPtr pvUser);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_profile(IntPtr db, NativeMethods.callback_profile func, IntPtr pvUser);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_progress_handler(IntPtr db, int instructions, NativeMethods.callback_progress_handler func, IntPtr pvUser);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_trace(IntPtr db, NativeMethods.callback_trace func, IntPtr pvUser);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_rollback_hook(IntPtr db, NativeMethods.callback_rollback func, IntPtr pvUser);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_db_handle(IntPtr stmt);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_next_stmt(IntPtr db, IntPtr stmt);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_stmt_busy(IntPtr stmt);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_stmt_readonly(IntPtr stmt);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_exec(IntPtr db, byte[] strSql, NativeMethods.callback_exec cb, IntPtr pvParam, out IntPtr errMsg);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_get_autocommit(IntPtr db);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_extended_result_codes(IntPtr db, int onoff);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_errcode(IntPtr db);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_extended_errcode(IntPtr db);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_errstr(int rc); /* 3.7.15+ */

		// Since sqlite3_log() takes a variable argument list, we have to overload declarations
		// for all possible calls.  For now, we are only exposing a single string, and 
		// depend on the caller to format the string.
		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate void sqlite3_log(int iErrCode, byte[] zFormat);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_file_control(IntPtr db, byte[] zDbName, int op, IntPtr pArg);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate IntPtr sqlite3_backup_init(IntPtr destDb, byte[] zDestName, IntPtr sourceDb, byte[] zSourceName);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_backup_step(IntPtr backup, int nPage);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_backup_finish(IntPtr backup);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_backup_remaining(IntPtr backup);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_backup_pagecount(IntPtr backup);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_blob_open(IntPtr db, byte[] sdb, byte[] table, byte[] col, long rowid, int flags, out IntPtr blob);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_blob_write(IntPtr blob, IntPtr b, int n, int offset);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_blob_read(IntPtr blob, IntPtr b, int n, int offset);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_blob_bytes(IntPtr blob);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_blob_close(IntPtr blob);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_wal_autocheckpoint(IntPtr db, int n);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_wal_checkpoint(IntPtr db, byte[] dbName);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_wal_checkpoint_v2(IntPtr db, byte[] dbName, int eMode, out int logSize, out int framesCheckPointed);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_set_authorizer(IntPtr db, NativeMethods.callback_authorizer cb, IntPtr pvUser);

		[UnmanagedFunctionPointer(CALLING_CONVENTION, CharSet=CharSet.Unicode)]
		public delegate int sqlite3_win32_set_directory (uint directoryType, string directoryPath);

		[UnmanagedFunctionPointer(CALLING_CONVENTION)]
		public delegate int sqlite3_create_function_v2(IntPtr db, byte[] strName, int nArgs, int nType, IntPtr pvUser, NativeMethods.callback_scalar_function func, NativeMethods.callback_agg_function_step fstep, NativeMethods.callback_agg_function_final ffinal, NativeMethods.callback_destroy fdestroy);

	}

    }
}