using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SP.Tools.Unity
{
	[DisallowMultipleComponent]
	///<summary>
	///单例类, 在脚本中加入 protected override void DestroyOrSave() => DontDestroyOnLoadSingleton(); 可以轻松的实现场景切换时不删除
	///</summary>
	public class SingletonClass<T> : MonoBehaviour, IReady where T : SingletonClass<T>
	{
		private static T _instance;

		public static T instance
		{
			get
			{
				if (!_instance)
					_instance = FindObjectOfType<T>();

				if (!_instance)
				{
					if (!Application.isPlaying)
						return null;

					SummonInstance();
					Debug.LogWarning($"不存在单例物体, 已生成 ({typeof(T)})");
				}

				return _instance;
			}
		}

		public static bool HasInstance() => _instance;

		public static void SummonInstance()
		{
			if (!Application.isPlaying)
				return;

			GameObject go = new($"{typeof(T).Name} (SingletonClonedAfter)")
			{
				isStatic = true
			};
			_instance = go.AddComponent<T>();
		}

		protected virtual void Awake()
		{
			DestroyOrSave();

			if (!_instance)
				_instance = (T)this;
		}

		protected virtual void DestroyOrSave()
		{
			if (FindObjectsOfType<T>().Length > 1)
				Destroy(gameObject);
		}

		protected void DontDestroyOnLoadSingleton()
		{
			if (FindObjectsOfType<T>().Length > 1)
				Destroy(gameObject);
			else
				DontDestroyOnLoad(gameObject);
		}

		protected virtual void Start()
		{
			ready = true;
		}

		public static async void WhenReady(Action<T> action)
		{
			if (action == null)
			{
				Debug.LogError($"{nameof(action)} 值为空");
				return;
			}

			while (!HasInstance())
				await Time.fixedDeltaTime;

			while (!instance.ready)
				await Time.fixedDeltaTime;

			action(instance);
		}

		public bool ready { get; protected set; }
	}
}
