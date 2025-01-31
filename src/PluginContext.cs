﻿/*
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2010-2013 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins.
 *
 * Zongsoft.Plugins is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with Zongsoft.Plugins; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 封装了有关插件特定的信息。
	/// </summary>
	public sealed class PluginContext : MarshalByRefObject
	{
		#region 成员变量
		private PluginTree _pluginTree;
		private PluginSetup _settings;
		private PluginApplicationContext _applicationContext;
		#endregion

		#region 构造函数
		internal PluginContext(PluginSetup settings, PluginApplicationContext applicationContext)
		{
			if(settings == null)
				throw new ArgumentNullException("settings");

			if(applicationContext == null)
				throw new ArgumentNullException("applicationContext");

			_settings = (PluginSetup)settings.Clone();
			_pluginTree = new PluginTree(this);
			_applicationContext = applicationContext;

			_settings.PropertyChanged += delegate
			{
				if(_pluginTree != null && _pluginTree.Status != PluginTreeStatus.None)
					throw new InvalidOperationException();
			};
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取当前插件运行时的唯一插件树对象。
		/// </summary>
		public PluginTree PluginTree
		{
			get
			{
				return _pluginTree;
			}
		}

		/// <summary>
		/// 获取加载的根插件集。
		/// </summary>
		public IEnumerable<Plugin> Plugins
		{
			get
			{
				return _pluginTree.Plugins;
			}
		}

		/// <summary>
		/// 获取当前插件运行时所属的应用程序上下文对象。
		/// </summary>
		public PluginApplicationContext ApplicationContext
		{
			get
			{
				return _applicationContext;
			}
		}

		/// <summary>
		/// 获取当前插件上下文对应的设置。
		/// </summary>
		public PluginSetup Settings
		{
			get
			{
				return _settings;
			}
		}

		/// <summary>
		/// 获取插件的隔离级别。
		/// </summary>
		public IsolationLevel IsolationLevel
		{
			get
			{
				return _settings.IsolationLevel;
			}
		}

		/// <summary>
		/// 获取当前工作台(主界面)对象。
		/// </summary>
		public IWorkbenchBase Workbench
		{
			get
			{
				return this.ResolvePath(this.Settings.WorkbenchPath) as IWorkbenchBase;
			}
		}
		#endregion

		#region 解析路径
		/// <summary>
		/// 根据指定的路径文本获取其对应的缓存对象或该对象的成员值。
		/// </summary>
		/// <param name="pathText">要获取的路径文本，该文本可以用过句点符号(.)表示缓存对象的成员名。</param>
		/// <returns>返回获取的缓存对象或其成员值。</returns>
		/// <exception cref="System.ArgumentNullException"><paramref name="pathText"/>参数为空或全空字符串。</exception>
		/// <remarks>
		/// 注意：成员名只能是公共的实例属性或字段。
		/// <example>/Workspace/Environment/ApplicationContext.ApplicationId</example>
		/// </remarks>
		public object ResolvePath(string pathText)
		{
			ObtainMode mode;
			return this.ResolvePath(PluginPath.PreparePathText(pathText, out mode), this.PluginTree.RootNode, mode, null);
		}

		internal object ResolvePath(string pathText, PluginTreeNode origin, ObtainMode obtainMode, Type targetType)
		{
			if(string.IsNullOrWhiteSpace(pathText))
				throw new ArgumentNullException(nameof(pathText));

			var expression = PluginPath.Parse(pathText);
			var node = origin.Find(expression.Path);

			if(node == null)
				throw new PluginException($"Not found the PluginTreeNode with '{expression.Path}' path.");

			try
			{
				//获取指定路径的目标对象
				object target = node.UnwrapValue(obtainMode, targetType == null ? null : new Builders.BuilderSettings(targetType));

				if(target != null && expression.Members.Length > 0)
					return Reflection.MemberAccess.GetMemberValue<object>(target, expression.Members);

				return target;
			}
			catch(Exception ex)
			{
				var fileName = string.Empty;

				if(origin != null && origin.Plugin != null)
					fileName = System.IO.Path.GetFileName(origin.Plugin.FilePath);

				throw new PluginException(string.Format("Resolve target error from '{0}' path in '{1}' plugin file.", pathText, fileName), ex);
			}
		}
		#endregion
	}
}
