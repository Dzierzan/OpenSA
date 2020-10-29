Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor130.CenterPosition

	Ants = Player.GetPlayer("Ants")
	Wasps = Player.GetPlayer("Wasps")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Ants.GrantCondition("enable-ants-ai")
		Wasps.GrantCondition("enable-wasps-ai")
		Spiders.GrantCondition("enable-spiders-ai")
	end)

end
