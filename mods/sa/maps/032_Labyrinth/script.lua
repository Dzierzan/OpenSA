Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor172.CenterPosition

	Ants = Player.GetPlayer("Ants")
	Beetles = Player.GetPlayer("Beetles")
	Wasps = Player.GetPlayer("Wasps")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Ants.GrantCondition("enable-ants-ai")
		Beetles.GrantCondition("enable-beetles-ai")
		Wasps.GrantCondition("enable-wasps-ai")
	end)

end
