namespace ItelexNewsServer.Data
{
	class InitChannels
	{
		private string[] _channels = new string[]
		{
			"@BBCBreaking",
			"@BBCNews",
			"@BBCWorld",
			//"@BBK",
			"@big_ben_clock",
			"@BLM_Bayern",
			"@BUNTE",
			"@Der_Postillon",
			"@dieternuhr",
			"@dpa",
			"@DracheStreamt",
			"@dwd_klima",
			"@FadingDe",
			"@FAZ_Eil",
			"@faznet",
			"@fckoeln_live",
			"@FeuerwehrOL",
			"@heisec",
			//"@ksta_news",
			"@mfk_heusenstamm",
			"@NASA",
			"@netzpolitik_org",
			"@NPCoburg",
			"@nytimes",
			"@POTUS",
			//"@realDonaldTrump",
			"@ReutersWorld",
			"@rheinbahn_intim",
			"@shentongroup_",
			//"@stromausfall_de",
			"@sz",
			"@tagesschau",
			"@tagesschau_eil",
			"@unwetteralarm",

			//"#telextweet",

			"Allgemein",
			"Systemmeldungen",
			"Telegrammdienst",
		};

		public void AddChannels()
		{
			foreach(string name in _channels)
			{
				ChannelItem item = new ChannelItem()
				{
					Name = name,
					Type = name.StartsWith("@") ? "tw" : "it",
					Active = true,
				};
				NewsDatabase.Instance.ChannelInsert(item);
			}
		}
	}
}
