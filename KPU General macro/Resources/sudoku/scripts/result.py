def callback(vmodel, frame, parameter):
    try:
        vmodel.App.Click(parameter['re game'])
        vmodel.InitStopWatch = True
        return None
    except Exception as e:
        return str(e)