using System;
using SP.Tools;

namespace GameCore
{
	public class FixedUpdateChineseNameAttribute : ChineseNameAttribute
	{
		public FixedUpdateChineseNameAttribute() : base("固定更新") { }
	}

	public class UpdateChineseNameAttribute : ChineseNameAttribute
	{
		public UpdateChineseNameAttribute() : base("更新") { }
	}

	public class StartChineseNameAttribute : ChineseNameAttribute
	{
		public StartChineseNameAttribute() : base("开始") { }
	}

	public class AwakeChineseNameAttribute : ChineseNameAttribute
	{
		public AwakeChineseNameAttribute() : base("唤醒") { }
	}
}
