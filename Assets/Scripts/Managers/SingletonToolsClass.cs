using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore.High
{
	///<summary>
	///自带 Tools 的单例类, 在脚本中加入 protected override void DestroyOrSave() => DontDestroyOnLoadSingleton(); 可以轻松的实现场景切换时不删除
	///</summary>
	public class SingletonToolsClass<T> : SingletonClass<T> where T : SingletonToolsClass<T>
	{
		public Tools tools => Tools.instance;
	}
}
