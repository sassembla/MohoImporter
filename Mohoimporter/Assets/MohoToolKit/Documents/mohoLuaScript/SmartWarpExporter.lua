-- **********S****************************************
-- Provide Moho with the name of this script object
-- **************************************************

ScriptName = "SmartWarpExporter"

-- **************************************************
-- General information about this script
-- **************************************************

SmartWarpExporter = {}

function SmartWarpExporter:Name()
	return "SmartWarpExporter"
end

function SmartWarpExporter:Version()
	return '1.0'
end

function SmartWarpExporter:Description()
	return "Export Color Channel"
end

function SmartWarpExporter:Creator()
	return 'cItoh'
end

function SmartWarpExporter:UILabel()
	return "SmartWarpExporter"
end

-- **************************************************
-- The guts of this script
-- **************************************************

function  SmartWarpExporter:Run(moho)

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
			--print("Layer --------- >> " .. layer:Name() .. " / " ..  LayerType[typeLy+1] )
			
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
--				print(tempLayer:Name() .. " Origin :" .. layer:Origin().x .. " , " ..  layer:Origin().y )
				
				--BoneLayerの収集
				local BoneLayer = layer:ControllingBoneLayer()				
				if BoneLayer then
					boneArray[#boneArray + 1] = BoneLayer
					--print("Layer Name :" .. tempLayer:Name() .. "  BoneLayer  :" .. BoneLayer:Name() .. " Skeleton :" .. tempLayer:ControllingSkeleton())
				end
				
				--Layerの情報調査
--				local vectorLayerDebug = moho:LayerAsVector(layer)
--				if vectorLayerDebug then
--					print("vectorLayer : " .. vectorLayerDebug:Name())
--					
--				end
				
--				local ancester = layer:AncestorSwitchChild()
--				if ancester then
--					print("ancesterLayerID : " .. ancester:Name())
--				end
				
--				local follow = layer:GetFollowingLayer()
--				if follow then
--					print("FollowingLayerID : " .. follow:Name())
--				end
			
--				local box = layer:Bounds(0)
--				if box then
--					print("Box Center >>> " .. box:Center2D().x .. "," ..box:Center2D().y )
--				end
				
--				local image = moho:LayerAsImage(layer)
--				if image then
--					print("imageWH :" .. image:Width() ..",".. image:Height() .. "imagePixelWH :" .. image:PixelWidth() .. "," .. image:PixelHeight())
--					local Bounds = image:CroppingBounds()
--					--print("BoundsCenter2D :" .. Bounds:Center2D().x .. " , " .. Bounds:Center2D().y)
--					--print(Bounds.fMax.x .. " , " .. Bounds.fMax.y .. " , " .. Bounds.fMax.z)
--					print("CountTrackingPoints :" .. image:CountTrackingPoints())
--					local ML = image:GetDistortionMeshLayer()
--					print("KoreKana?   >> " .. ML:Name() .. " | " .. moho.document:LayerAbsoluteID(ML) .. " | " .. moho.document:LayerAbsoluteID(image))
--				end
				
				--これ！
				--イメージレイヤーとSmartWarpレイヤー間ではこの数値を　Image-SmartWarp　で差し引きした数値を、頂点の座標に入れこむ必要がある。
--				print("fTranslation: ( " .. layer.fTranslation.value.x .. " , " .. layer.fTranslation.value.y .. " , " .. layer.fTranslation.value.z .. " )")
--				local follow = layer:GetFollowingLayer()
--				if follow then
--					print("GetFollowingLayer :" .. layer:GetFollowingLayer())
--				end
--				print("Following" .. layer.fFollowing.value)
				--情報調査ここまで
				
--				repeat
--					local parentLayerGroup = tempLayer:Parent()
					
--					if parentLayerGroup then
--						local parentLayer = parentLayerGroup:Layer(0)
--						local groupCount = parentLayerGroup:CountLayers()
--						print("LAYERNAME :" .. tempLayer:Name() .. "    GROUPED NO 0 IS:" ..parentLayer:Name())
--				    end
				    
--					local tempParent = tempLayer:Parent():Layer():Name()
					
--					if tempParent then
--						local tempParentName = tempParent:Name()
--						
						--一回ここでRootか判定する
--						if tempParent:Parent() then
---							tempParentName = tempParent .. "|" .. (UniqueCount-1)
--						end
--						
--						ObjectName = tempParentName .. "/" .. ObjectName
--						tempLayer = tempParent
--					else
--						--今のObjectがrootだった時
--						ObjectName = ObjectName .."|".. moho.document:LayerAbsoluteID(tempLayer)-1
--					end
--				until not tempParent
				
				--ObjectName = tempLayer:Name() .. "|" .. moho.document:LayerAbsoluteID(tempLayer)-1
				--ObjectName = tempLayer:Name() .. "|" .. (UniqueCount-1)
				ObjectName = tempLayer:Name()
				
				local image = moho:LayerAsImage(layer)
				if (image) and (layer:IsVisible()) then
					
					local ML = image:GetDistortionMeshLayer()
					local MLname = ""
					if ML then
						MLname = ML:Name()
					end
					
--					print("imageWH :" .. image:Width() ..",".. image:Height() .. "imagePixelWH :" .. image:PixelWidth() .. "," .. image:PixelHeight())
					stringData[#stringData +1] = "    {\n"
					stringData[#stringData +1] = "        \"LayerType\" : \"IMAGE\",\n"
					stringData[#stringData +1] = "        \"ObjectName\" : \"" .. ObjectName .. "|" .. (UniqueCount-1) .. "\",\n"
					if MLname == "" then
						stringData[#stringData +1] = "        \"LinkedLayer\" : \"NULL\",\n"
					else
						stringData[#stringData +1] = "        \"LinkedLayer\" : \"" .. MLname .. "|" .. moho.document:LayerAbsoluteID(ML)-1 .. "\",\n"
						--stringData[#stringData +1] = "        \"LinkedLayer\" : \"" .. MLname .. "|" .. (UniqueCount-1) .. "\",\n"
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
					--stringData[#stringData +1] = "    },\n"
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
				
--				print("Following" .. layer.fFollowing.value)
			
			--頂点データの抜き出し
			for i=0,numCh -6 do
				local chInfo =  MOHO.MohoLayerChannel:new_local()
				layer:GetChannelInfo(i,chInfo)
				
				if not chInfo.selectionBased then
				
					--print("    " .. chInfo.name:Buffer() .. " / " .. chInfo.subChannelCount)
					
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
						
						--stringData[#stringData +1] = "    {\n"
						--stringData[#stringData +1] = "        \"ObjectName\" : \"" .. ObjectName .. "\",\n"
						--stringData[#stringData +1] = "        \"ChannelName\" : \"" .. chInfo.name:Buffer() .. "\",\n"
						--stringData[#stringData +1] = "        \"PointAnimation\": [\n"
						
						
						local keyNo = ""
						for subID=0,chInfo.subChannelCount -1 do
							local subChannel = layer:Channel(i,subID,moho.document)
							local channel = null
							--print(ChannelType[subChannel:ChannelType() + 1])
							
							if subChannel:ChannelType() == MOHO.CHANNEL_VEC2  then
								channel = moho:ChannelAsAnimVec2(subChannel)
								--print( LayerType[typeLy+1])
								keyNo = keyNo ..  "[" .. subID .. "]".. channel:CountKeys() .. " , "
								
								if (subChannel:CountKeys() >= 1) then
									tempString = tempString .. "            {\"PointIndex\" : " .. subID .. ",\n"
									tempString = tempString .. "            \"KeyLength\" : " ..subChannel:CountKeys() ..",\n"
									tempString = tempString .. "            \"Duration\" : " ..subChannel:Duration() ..",\n"
									tempString = tempString .. "            \"AnimationKey\": [\n"
									
									--stringData[#stringData +1] = "            {\"PointIndex\" : " .. subID .. ",\n"
									--stringData[#stringData +1] = "            \"KeyLength\" : " ..subChannel:CountKeys() ..",\n"
									--stringData[#stringData +1] = "            \"Duration\" : " ..subChannel:Duration() ..",\n"
									--stringData[#stringData +1] = "            \"AnimationKey\": [\n"
									
									for l=0,subChannel:CountKeys()-1 do
										local targetFrame = subChannel:GetKeyWhen(l)
										local currentVal = channel:GetValue(targetFrame)
										tempString = tempString .. "                {\"Frame\" : " .. targetFrame .. ", \"value\" : [" .. currentVal.x .. " , ".. currentVal.y .."]},\n"
										--stringData[#stringData +1] = "                {\"Frame\" : " .. targetFrame .. ", \"value\" : [" .. currentVal.x .. " , ".. currentVal.y .."]},\n";
									end
									tempString = tempString .. "            ]},\n"
									--stringData[#stringData +1] = "            ]},\n"
								end
								--currentVal = channel:GetValue(moho.frame)
								--stringData[#stringData +1] = currentVal.x .. " , ".. currentVal.y .."\n"
							end
						end
						tempString = tempString .. "        ],\n"
						--stringData[#stringData +1] = "        ],\n"
						
						--Triangle情報の所得
						local triangle = ""
						local triangleCount = ""
						if typeLy == MOHO.LT_VECTOR then
			--				print("VectorType")
							local vectorLayer = moho:LayerAsVector(layer)
							local mesh = vectorLayer:Mesh()
							
							local Nomm =  vectorLayer:FillTexture()
							
							if Nomm then
			--					print("FillTexture :" ..  Nomm)
							end
							
							if not mesh then
			--					print("No Mesh")
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
										--print(pointID .. "     fPos:" .. point.fPos.x ..", ".. point.fPos.y .. "     fAnimPos:" .. point.fAnimPos.value.x .. ", " .. point.fAnimPos.value.y )
									end
									--print ("MeshID:" .. s ..  "     Points:" .. nPoint .. "    Triangle:" .. temp)
									triangle = triangle .. temp
								end
							end
							
						end
						
						tempString = tempString .. "        \"Triangle\" : [" .. triangle .. "],\n"
						tempString = tempString .. "        \"TriangleCount\" : [" .. triangleCount .. "] \n"
						
						tempString = tempString .. "    },\n"
						--stringData[#stringData +1] = "    },\n"
						
						--Point情報がない場合はなかったことにする
						if(chInfo.subChannelCount == 0) then
							tempString = ""
						end
						--print(keyNo)
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
	--				print(fixedBoneLayer[a]:Name() .. " in index :" .. b .. "  Bone Name : " .. name .. " Parent index :" .. parent)
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
	
	--local path = moho:UserAppDir() .. "\\scripts\\tool\\" .. fileName
	local file = io.open(filePath .. ".json","w")
	
	
	for i=1,#stringData do
		--print(stringData[i])
		file:write(stringData[i])
	end
	
	--file:write(triangle .. "\n")
	io.close(file)

--	local numCh = moho.layer:CountChannels()
--	print("Number of animation channels in current layer: " .. numCh)

--	for i = 0, numCh - 1 do
--		local chInfo = MOHO.MohoLayerChannel:new_local()
--		moho.layer:GetChannelInfo(i, chInfo)
--		
--		if not chInfo.selectionBased then
--			local subChannelBase = moho.layer:Channel(i,0,moho.document)
--			
--			if not subChannelBase.selectionBased then
--				print("Channel " .. i .. ": " .. chInfo.name:Buffer() .. " Keyframes: " .. subChannelBase:CountKeys())
--				
--				for subID=0,chInfo.subChannelCount -1 do
--					local subChannel = moho.layer:Channel(i,subID,moho.document)
--					local channel = null
--					if subChannel:ChannelType() == MOHO.CHANNEL_COLOR then
--						channel = moho:ChannelAsAnimColor(subChannel)
--						if channel then
--							currentVal = channel:GetValue(moho.frame)
--							print("    Sub-channel " .. subID .. " Keyframes: " .. subChannel:CountKeys() .." currentVal: ( "
--								 .. currentVal.r .. " , " .. currentVal.g .. " , " .. currentVal.b .. " , " .. currentVal.a .. " )")
--						end
--					end
--				end
--			end
--		end
--	end

--		if (chInfo.subChannelCount == 1) then
--			local ch = moho.layer:Channel(i, 0, moho.document)
--			print("Channel " .. i .. ": " .. chInfo.name:Buffer() .. "Keyframes: " .. ch:CountKeys())
--		else
--			print("Channel " .. i .. ": " .. chInfo.name:Buffer())
--			for subID = 0, chInfo.subChannelCount - 1 do
--				local ch = moho.layer:Channel(i, subID, moho.document)
--				print("    Sub-channel " .. subID .. " Keyframes: " .. ch:CountKeys())
--			end
--		end
--	end

end

local function getParentPath(_path) 
    pattern1 = "^(.+)//" 
    pattern2 = "^(.+)\\" 

    if (string.match(path,pattern1) == nil) then 
     return string.match(path,pattern2) 
    else 
     return string.match(path,pattern1) 
    end 
end 
