Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor150.CenterPosition

	Ants = Player.GetPlayer("Ants")
	Wasps = Player.GetPlayer("Wasps")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Ants.GrantCondition("enable-ants-ai")
		Wasps.GrantCondition("enable-wasps-ai")
	end)

end
