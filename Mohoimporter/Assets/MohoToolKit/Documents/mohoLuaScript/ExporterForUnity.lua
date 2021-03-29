-- **************************************************
-- Provide Moho with the name of this script object
-- **************************************************

ScriptName = "ExporterForUnity"

-- **************************************************
-- General information about this script
-- **************************************************

ExporterForUnity = {}

function ExporterForUnity:Name()
	return self:Localize('UILabel')
end

function ExporterForUnity:Version()
	return '0.5'
end

function ExporterForUnity:UILabel()
	return self:Localize('UILabel')
end

function ExporterForUnity:Creator()
	return 'cItoh'
end

function ExporterForUnity:Description()
	return self:Localize('Description')
end

function ExporterForUnity:ColorizeIcon()
	return true
end

-- **************************************************
-- Is Relevant / Is Enabled
-- **************************************************

function ExporterForUnity:IsRelevant(moho)
	return true
end

function ExporterForUnity:IsEnabled(moho)
	return true
end

-- **************************************************
-- Keyboard/Mouse Control
-- **************************************************

function ExporterForUnity:OnMouseDown(moho, mouseEvent)
	
end

function ExporterForUnity:OnMouseMoved(moho, mouseEvent)
	
end

function ExporterForUnity:OnMouseUp(moho, mouseEvent)
	
end

function ExporterForUnity:OnKeyDown(moho, keyEvent)
	
end

function ExporterForUnity:OnKeyUp(moho, keyEvent)
	
end

-- **************************************************
-- Tool Panel Layout
-- **************************************************

ExporterForUnity.EXPORT = MOHO.MSG_BASE

function ExporterForUnity:DoLayout(moho, layout)
    self.dynamicText1Text = LM.GUI.DynamicText(self:Localize('Exporter For Unity'), 0)
    layout:AddChild(self.dynamicText1Text, LM.GUI.ALIGN_CENTER, 5)
    
	self.exportButton = LM.GUI.Button(self:Localize('Export'), self.EXPORT)
	layout:AddChild(self.exportButton, LM.GUI.ALIGN_LEFT, 0)
end

function ExporterForUnity:HandleMessage(moho, view, msg)
	if msg == self.EXPORT then
		print('Message EXPORT received')
		
		local LayerType = {"UNKNOWN","VECTOR","IMAGE","GROUP","BONE","SWITCH","PARTICLE","NOTE","3D","AUDIO","PATCH","TEXT"}
		local ChannelType = {"UNKNOWN","VAL","VEC2","VEC3","COLOR","BOOL","STRING"}

		local count = 0
		local UniqueCount = 0
		local stringData = {}
	
		stringData[#stringData +1] = "{" .. "\n"
		stringData[#stringData +1] = "    \"FrameRate\" : " .. moho.document:Fps() .. ",\n"
		stringData[#stringData +1] = "    \"StartFrame\" : " .. moho.document:StartFrame() .. ",\n"
		stringData[#stringData +1] = "    \"EndFrame\" : " .. moho.document:EndFrame() .. ",\n"
		stringData[#stringData +1] = "    \"AnimationList\" : [" .. "\n"
	
		--これフルパス！！
		print(moho.document:Path())
		print("Screen Size: " .. moho.document:Width() .. "," .. moho.document:Height())
	
		local boneArray = {}
		
		repeat
			local layer = moho.document:LayerByAbsoluteID(count)

			if layer then
				count = count + 1
				
				local numCh = layer:CountChannels()
				local typeLy = layer:LayerType()
				
				if (typeLy == MOHO.LT_VECTOR) or (typeLy == MOHO.LT_IMAGE) or (typeLY == MOHO.LT_3D) or (typeLy == MOHO.LT_TEXT) then
					if(layer:IsVisible())then
						if(typeLy == MOHO.LT_VECTOR)  then
							--このVectorレイヤーにシェイプがあるか確認します
							local meshL = moho:LayerAsVector(layer)
							if meshL then
								local meshCheck = meshL:Mesh()
								if meshCheck then
									local count = meshCheck:CountShapes()
									if (count >= 1) then
										-- IsEditOnlyはみておかないと、普通のMeshとSmartWarpのMeshの区別がつかない
										if(layer:IsEditOnly()) then
											print("[" .. UniqueCount - 1 .. "]  " .. layer:Name() .. tostring(layer:IsEditOnly()))
										else
										--falseの時だけカウントする
											UniqueCount = UniqueCount + 1
											print("[" .. UniqueCount - 1 .. "]  " .. layer:Name() .. tostring(layer:IsEditOnly()))
										end
									else
										print("[" .. UniqueCount - 1 .. "]  " .. layer:Name())
									end
								else
									print("[" .. UniqueCount - 1 .. "]  " .. layer:Name())
								end
							else
								print("[" .. UniqueCount - 1 .. "]  " .. layer:Name())
							end
						end
					
						if(typeLy == MOHO.LT_IMAGE) or (typeLy == MOHO.LT_TEXT) then
							UniqueCount = UniqueCount + 1
							print("[" .. UniqueCount - 1 .. "]  " .. layer:Name())
						end
					end
				end
				
				--階層の所得
					local tempLayer = layer
					local ObjectName = tempLayer:Name()
					
					--BoneLayerの収集
					local BoneLayer = layer:ControllingBoneLayer()				
					if BoneLayer then
						boneArray[#boneArray + 1] = BoneLayer
						--print("Layer Name :" .. tempLayer:Name() .. "  BoneLayer  :" .. BoneLayer:Name() .. " Skeleton :" .. tempLayer:ControllingSkeleton())
					end
					
					ObjectName = tempLayer:Name()
					
					local image = moho:LayerAsImage(layer)
					if (image) and (layer:IsVisible()) then
						
						local ML = image:GetDistortionMeshLayer()
						local MLname = ""
						if ML then
							MLname = ML:Name()
						end
						
						stringData[#stringData +1] = "    {\n"
						stringData[#stringData +1] = "        \"LayerType\" : \"IMAGE\",\n"
						stringData[#stringData +1] = "        \"ObjectName\" : \"" .. ObjectName .. "|" .. (UniqueCount-1) .. "\",\n"
						if MLname == "" then
							stringData[#stringData +1] = "        \"LinkedLayer\" : \"NULL\",\n"
						else
							stringData[#stringData +1] = "        \"LinkedLayer\" : \"" .. MLname .. "|" .. moho.document:LayerAbsoluteID(ML)-1 .. "\",\n"
						end
						if BoneLayer then
							stringData[#stringData +1] = "        \"BoneLayer\" : \"" .. BoneLayer:Name() .. "\",\n"
						else
							stringData[#stringData +1] = "        \"BoneLayer\" : \"NULL\",\n"
						end
						stringData[#stringData +1] = "        \"Translation\" : {\n"
						stringData[#stringData +1] = "            \"x\" : " .. layer.fTranslation.value.x .. ",\n"
						stringData[#stringData +1] = "            \"y\" : " .. layer.fTranslation.value.y .. ",\n"
						stringData[#stringData +1] = "            \"z\" : " .. layer.fTranslation.value.z .. "\n"
						stringData[#stringData +1] = "        }\n"
						stringData[#stringData +1] = "        \"LayerScale\":{\n"
						stringData[#stringData +1] = "            \"x\" : " .. layer.fScale.value.x .. ",\n"
						stringData[#stringData +1] = "            \"y\" : " .. layer.fScale.value.y .. ",\n"
						stringData[#stringData +1] = "            \"z\" : " .. layer.fScale.value.z .. "\n"
						stringData[#stringData +1] = "        },\n"
						local tempKey = ""
						for i=0,numCh -6 do
							local chInfo = MOHO.MohoLayerChannel:new_local()
							layer:GetChannelInfo(i,chInfo)
							if not chInfo.selectionBased then
								if(chInfo.name:Buffer() == "Layer Opacity") then
									tempKey = tempKey .. "        \"ChannelName\" : \"" .. chInfo.name:Buffer() .. "\",\n"
									tempKey = tempKey .. "        \"OpacityAnimation\": [\n"
									for subID=0,chInfo.subChannelCount -1 do
										local subChannel = layer:Channel(i,subID,moho.document)
										local channel = null
										
										if subChannel:ChannelType() == MOHO.CHANNEL_VAL  then
											channel = moho:ChannelAsAnimVal(subChannel)
											if (subChannel:CountKeys() >= 1) then
												tempKey = tempKey .. "            {\"PointIndex\" : " .. subID .. ",\n"
												tempKey = tempKey .. "            \"KeyLength\" : " ..subChannel:CountKeys() ..",\n"
												tempKey = tempKey .. "            \"Duration\" : " ..subChannel:Duration() ..",\n"
												tempKey = tempKey .. "            \"AnimationKey\": [\n"
										
												for l=0,subChannel:CountKeys()-1 do
													local targetFrame = subChannel:GetKeyWhen(l)
													local currentVal = channel:GetValue(targetFrame)
													print("Frame:" .. targetFrame .. " Value: " .. currentVal)
													tempKey = tempKey .. "                {\"Frame\" : " .. targetFrame .. ", \"value\" : " .. currentVal .."},\n"
												end
												tempKey = tempKey .. "            ]},\n"
											end
										end
									end
									tempKey = tempKey .. "        ],\n"
								end
							end
						end
						stringData[#stringData +1] = tempKey
						stringData[#stringData +1] = "    },\n"
						
					end

				--頂点データの抜き出し
				for i=0,numCh -6 do
					local chInfo =  MOHO.MohoLayerChannel:new_local()
					layer:GetChannelInfo(i,chInfo)
					
					if not chInfo.selectionBased then
						
						--PointMotion
						if(chInfo.name:Buffer() == "Point Motion") then
							local tempString = "    {\n"
							tempString = tempString .. "        \"LayerType\" : \"VECTOR\",\n"
							tempString = tempString .. "        \"ObjectName\" : \"" .. ObjectName .. "|"  .. moho.document:LayerAbsoluteID(tempLayer)-1 .. "\",\n"
							if BoneLayer then
								tempString = tempString .. "        \"BoneLayer\" : \"" .. BoneLayer:Name() .. "\",\n"
							else
								tempString = tempString .. "        \"BoneLayer\" : \"NULL\",\n"
							end
							tempString = tempString .. "        \"Translation\" : {\n"
							tempString = tempString .. "            \"x\" : " .. layer.fTranslation.value.x .. ",\n"
							tempString = tempString .. "            \"y\" : " .. layer.fTranslation.value.y .. ",\n"
							tempString = tempString .. "            \"z\" : " .. layer.fTranslation.value.z .. "\n"
							tempString = tempString .. "        },\n"
							tempString = tempString .. "        \"LayerScale\" : {\n"
							tempString = tempString .. "            \"x\" : " .. layer.fScale.value.x .. ",\n"
							tempString = tempString .. "            \"y\" : " .. layer.fScale.value.y .. ",\n"
							tempString = tempString .. "            \"z\" : " .. layer.fScale.value.z .. "\n"
							tempString = tempString .. "        },\n"
							tempString = tempString .. "        \"ChannelName\" : \"" .. chInfo.name:Buffer() .. "\",\n"
							tempString = tempString .. "        \"PointAnimation\": [\n"
							
							local keyNo = ""
							for subID=0,chInfo.subChannelCount -1 do
								local subChannel = layer:Channel(i,subID,moho.document)
								local channel = null
								
								if subChannel:ChannelType() == MOHO.CHANNEL_VEC2  then
									channel = moho:ChannelAsAnimVec2(subChannel)
									keyNo = keyNo ..  "[" .. subID .. "]".. channel:CountKeys() .. " , "
									
									if (subChannel:CountKeys() >= 1) then
										tempString = tempString .. "            {\"PointIndex\" : " .. subID .. ",\n"
										tempString = tempString .. "            \"KeyLength\" : " ..subChannel:CountKeys() ..",\n"
										tempString = tempString .. "            \"Duration\" : " ..subChannel:Duration() ..",\n"
										tempString = tempString .. "            \"AnimationKey\": [\n"
																			
										for l=0,subChannel:CountKeys()-1 do
											local targetFrame = subChannel:GetKeyWhen(l)
											local currentVal = channel:GetValue(targetFrame)
											tempString = tempString .. "                {\"Frame\" : " .. targetFrame .. ", \"value\" : [" .. currentVal.x .. " , ".. currentVal.y .."]},\n"
										end
										tempString = tempString .. "            ]},\n"
									end
								end
							end
							tempString = tempString .. "        ],\n"
							
							--Triangle情報の所得
							local triangle = ""
							local triangleCount = ""
							if typeLy == MOHO.LT_VECTOR then
								local vectorLayer = moho:LayerAsVector(layer)
								local mesh = vectorLayer:Mesh()

								if not mesh then
								else
									for s=0,mesh:CountShapes()-1 do
										local nextShape = mesh:Shape(s)
										local nPoint = nextShape:CountPoints()
										local temp = ""
										triangleCount = triangleCount ..  "" .. nPoint .. ","
										for j=0,nPoint-1 do
											local pointID = nextShape:GetPoint(j)
											temp = temp .. pointID .. ","
											local point = mesh:Point(pointID)
										end
										--print ("MeshID:" .. s ..  "     Points:" .. nPoint .. "    Triangle:" .. temp)
										triangle = triangle .. temp
									end
								end
								
							end
							
							tempString = tempString .. "        \"Triangle\" : [" .. triangle .. "],\n"
							tempString = tempString .. "        \"TriangleCount\" : [" .. triangleCount .. "] \n"
							tempString = tempString .. "    },\n"
							
							--Point情報がない場合はなかったことにする
							if(chInfo.subChannelCount == 0) then
								tempString = ""
							end
							stringData[#stringData +1] = tempString
						end
						
						
					end
				end
			end
		until not layer  
		stringData[#stringData +1] = "    ],\n"
	
		--BoneLayerの整理
		if boneArray then
			local fixedBoneLayer = {}
			fixedBoneLayer[#fixedBoneLayer +1] = boneArray[1]
			for i=1,#boneArray,1 do		
				if fixedBoneLayer[#fixedBoneLayer] == boneArray[i] then
				else
					fixedBoneLayer[#fixedBoneLayer +1] = boneArray[i]
				end
			end
			print("BoneLayer Count :" .. #fixedBoneLayer)
			
			if (#fixedBoneLayer ~= 0) then
				--BoneLayerの中さぐるよ
				stringData[#stringData + 1] = "    \"Bonetree\" : [\n"
				for a=1,#fixedBoneLayer do
					local skel = fixedBoneLayer[a]:Skeleton()
					stringData[#stringData +1] = "      {\"LayerName\" : \"" .. fixedBoneLayer[a]:Name() .. "\",\n"
					stringData[#stringData +1] = "      \"BoneArray\" : [\n"
					for b=0,skel:CountBones()-1 do
						local bone = skel:Bone(b)
						local name = bone:Name()
						local parent = bone.fParent
						stringData[#stringData +1] = "        {\"BoneName\" : \"" .. name .. "\",\n"
						stringData[#stringData +1] = "        \"ParentIndex\" : \"" .. parent .. "\"},\n"
					end
					stringData[#stringData +1] = "    ]},\n"
				end
				stringData[#stringData +1] = "  ]\n"
			end
		end
	
		stringData[#stringData +1] = "}\n"
	
		local fileName = moho.document:Name()
		fileName = string.reverse(fileName)
		placeNum = fileName:find("ohom.")
		fileName = string.reverse(fileName)
		fileName = string.sub(fileName,0,#fileName - (placeNum+4))
	
		print(moho.document:Path())
		print(fileName)
		print(moho:UserAppDir())
	
		local documentPath = moho.document:Path()
		local filePath = ""
		local pattern1 = "^(.+)/" 
		local pattern2 = "^(.+)\\" 
		if (string.match(documentPath,pattern1) == nil) then 
			filePath = string.match(documentPath,pattern2) .. "/" .. fileName
		else
			filePath = string.match(documentPath,pattern1)  .. "/" .. fileName
		end
	
		print(filePath)	
		local file = io.open(filePath .. ".json","w")
		
		for i=1,#stringData do
			file:write(stringData[i])
		end
		io.close(file)		
		
	else
		
	end
end

-- **************************************************
-- Localization
-- **************************************************

function ExporterForUnity:Localize(text)
	local phrase = {}

	phrase['Description'] = ''
	phrase['UILabel'] = 'ExporterForUnity'

	phrase['Exporter For Unity'] = 'Exporter For Unity'
	phrase['Export'] = 'Export'

	local fileWord = MOHO.Localize("/Menus/File/File=File")
	if fileWord == "Файл" then
		phrase['Description'] = 'Test Test'
		phrase['UILabel'] = 'ExporterForUnity'

		phrase['Exporter For Unity'] = 'Exporter For Unity'
		phrase['Export'] = 'Export'
	end

	return phrase[text]
end
