Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor71.CenterPosition

	Wasps = Player.GetPlayer("Wasps")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(1), function()
		Wasps.GrantCondition("enable-wasps-ai")
		Spiders.GrantCondition("enable-spiders-ai")
	end)

end
