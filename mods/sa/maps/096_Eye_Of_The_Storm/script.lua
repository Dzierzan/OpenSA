Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor234.CenterPosition

	Wasps = Player.GetPlayer("Wasps")
	Beetles = Player.GetPlayer("Beetles")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Wasps.GrantCondition("enable-wasps-ai")
		Beetles.GrantCondition("enable-beetles-ai")
		Spiders.GrantCondition("enable-spiders-ai")

	end)

end
