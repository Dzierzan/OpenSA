Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor46.CenterPosition

	Beetles = Player.GetPlayer("Beetles")
	Wasps = Player.GetPlayer("Wasps")
	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Beetles.GrantCondition("enable-beetles-ai")
		Wasps.GrantCondition("enable-wasps-ai")
		Spiders.GrantCondition("enable-spiders-ai")
	end)

end
