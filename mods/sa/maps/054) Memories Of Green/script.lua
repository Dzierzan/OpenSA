Tick = function()

end

WorldLoaded = function()

	Camera.Position = Actor372.CenterPosition

	Spiders = Player.GetPlayer("Spiders")
	Trigger.AfterDelay(DateTime.Seconds(6), function()
		Spiders.GrantCondition("enable-spiders-ai")

		Actor312.Attack(Actor189)
		Actor313.Attack(Actor189)

		Actor353.Attack(Actor188)
		Actor319.Attack(Actor188)
		Actor355.Attack(Actor188)
		Actor318.Attack(Actor188)
		Actor354.Attack(Actor188)
		Actor320.Attack(Actor188)
		Actor317.Attack(Actor188)
		Actor321.Attack(Actor188)
		Actor316.Attack(Actor188)
		Actor315.Attack(Actor188)
		Actor314.Attack(Actor188)
		Actor322.Attack(Actor188)
		Actor307.Attack(Actor188)
		Actor306.Attack(Actor188)
		Actor323.Attack(Actor188)
		Actor292.Attack(Actor188)
		Actor297.Attack(Actor188)
		Actor305.Attack(Actor188)
		Actor304.Attack(Actor188)
		Actor298.Attack(Actor188)
		Actor290.Attack(Actor188)
		Actor308.Attack(Actor188)
		Actor299.Attack(Actor188)
		Actor293.Attack(Actor188)
		Actor302.Attack(Actor188)
		Actor311.Attack(Actor188)
		Actor294.Attack(Actor188)
		Actor295.Attack(Actor188)
		Actor300.Attack(Actor188)
		Actor301.Attack(Actor188)
		Actor303.Attack(Actor188)
		Actor352.Attack(Actor188)
		Actor289.Attack(Actor188)
		Actor291.Attack(Actor188)
		Actor296.Attack(Actor188)
		Actor306.Attack(Actor188)
		Actor310.Attack(Actor188)

	end)

end
