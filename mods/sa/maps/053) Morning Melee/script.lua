Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor198.CenterPosition

	Wasps = Player.GetPlayer("Wasps")
	Beetles = Player.GetPlayer("Beetles")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Wasps.GrantCondition("enable-wasps-ai")
		Beetles.GrantCondition("enable-beetles-ai")
	end)

end
