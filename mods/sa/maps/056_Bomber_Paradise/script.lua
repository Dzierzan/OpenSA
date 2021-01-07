Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor188.CenterPosition

	Spiders = Player.GetPlayer("Spiders")
	Wasps = Player.GetPlayer("Wasps")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Spiders.GrantCondition("enable-spiders-ai")
		Wasps.GrantCondition("enable-wasps-ai")

	end)

end
