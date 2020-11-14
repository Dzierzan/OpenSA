Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor188.CenterPosition

	Wasps = Player.GetPlayer("Wasps")
	Ants = Player.GetPlayer("Ants")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Ants.GrantCondition("enable-ants-ai")
		Wasps.GrantCondition("enable-wasps-ai")

	end)

end
