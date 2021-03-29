-- **********S****************************************
-- Provide Moho with the name of this script object
-- **************************************************

ScriptName = "ColorAnimationExporter"

-- **************************************************
-- General information about this script
-- **************************************************

ColorAnimationExporter = {}

function ColorAnimationExporter:Name()
	return "ColorAnimationExporter"
end

function ColorAnimationExporter:Version()
	return '1.0'
end

function ColorAnimationExporter:Description()
	return "Export Color Channel"
end

function ColorAnimationExporter:Creator()
	return 'cItoh'
end

function ColorAnimationExporter:UILabel()
	return "ColorAnimationExporter"
end

-- **************************************************
-- The guts of this script
-- **************************************************

function  ColorAnimationExporter:Run(moho)
	local LayerType = {"UNKNOWN","VECTOR","IMAGE","GROUP","BONE","SWITCH","PARTICLE","NOTE","3D","AUDIO","PATCH","TEXT"}

	local count = 0	
	--print("--------------json------------------")
	local jsonData = {}
	
	--print("{")
	--print("    \"AnimationList\" : [")
	
	jsonData[#jsonData +1] = "{"
	jsonData[#jsonData +1] = "    \"FrameRate\" : " .. moho.document:Fps()
	jsonData[#jsonData +1] = "    \"StartFrame\" : " .. moho.document:StartFrame()
	jsonData[#jsonData +1] = "    \"EndFrame\" : " .. moho.document:EndFrame()
	jsonData[#jsonData +1] = "    \"AnimationList\" : ["
	
	--Layerの階層を所得します
	
	repeat
		local layer = moho.document:LayerByAbsoluteID(count)

		if layer then
			count = count + 1
			
			local numCh = layer:CountChannels()
			local typeLy = layer:LayerType()
			
			-- 3D Image Vector TEXT以外のレイヤーは扱わないことにします
			if (typeLy == MOHO.LT_VECTOR) or (typeLy == MOHO.LT_IMAGE) or (typeLY == MOHO.LT_3D) or (typeLy == MOHO.LT_TEXT) then
				
				--階層の所得
				local tempLayer = layer
				local ObjectName = tempLayer:Name()

				repeat
					local tempParent = tempLayer:Parent()
					if tempParent then
						local tempParentName = tempParent:Name()
						
						--一回ここでRootか判定する
						if tempParent:Parent() then
							tempParentName = tempParent .. "|" .. (count-1)
						end
						
						ObjectName = tempParentName .. "/" .. ObjectName
						tempLayer = tempParent
					else
						--今のObjectがrootだった時
						ObjectName = ObjectName .."|".. (count-1)
					end
				until not tempParent
				
				--Debug
				--if(layer:Name() == "FillColor") then
				--print("LayerName :" .. ObjectName .. " LayerType :" .. LayerType[typeLy+1] .. " : Channels " .. numCh)
				
				-- Camera用の４つとAllChannelは検索から除外しておきます 検索数が膨大になるので
				for i=0,numCh -6 do
					local chInfo =  MOHO.MohoLayerChannel:new_local()
					layer:GetChannelInfo(i,chInfo)
					
					--出力用のまとめテキストにします
					local outputData = {}
					local isColorChannel = false
					
					if not chInfo.selectionBased then
					
						--outputData[#outputData +1] = "[" .. i .. " ] " .. chInfo.name:Buffer() .. " : " .. chInfo.subChannelCount
						outputData[#outputData +1] = "    {"
						outputData[#outputData +1] = "        \"ObjectName\" : \"" .. ObjectName .. "\","
						outputData[#outputData +1] = "        \"ChannelName\" : \"" .. chInfo.name:Buffer() .. "\","
						-- print("[" .. i .. " ] " .. chInfo.name:Buffer() .. " : " .. chInfo.subChannelCount)
						
						for j=0, chInfo.subChannelCount-1 do
							local subChannel = layer:Channel(i,j,moho.document)
							local channel = null
							if subChannel:ChannelType() == MOHO.CHANNEL_COLOR then
								local channel = moho:ChannelAsAnimColor(subChannel)
								
								if (subChannel:CountKeys() > 1) then
									--outputData[#outputData +1] = chInfo.name:Buffer() .." sub-ChannelID: " .. j .. " Keyframes: " .. subChannel:CountKeys()
									outputData[#outputData +1] = "        \"KeyLength\" : " ..subChannel:CountKeys() ..","
									--print(chInfo.name:Buffer() .."  sub-ChannelID: " .. j .. " Keyframes: " .. subChannel:CountKeys())
									outputData[#outputData +1]= "        \"Duration\" : " ..subChannel:Duration() ..","
									outputData[#outputData+1] = "        \"AnimationKey\": ["
									if (subChannel:CountKeys() > 1) then
										isColorChannel = true
										for l=0,subChannel:CountKeys()-1 do
											local targetFrame = subChannel:GetKeyWhen(l)
											local currentVal = channel:GetValue(targetFrame)
									
											--outputData[#outputData+1] = "Frame:" .. subChannel:GetKeyWhen(l) .. " value:" .. currentVal.r .. " , " .. currentVal.g .. " , " .. currentVal.b .. " , " .. currentVal.a
											
											outputData[#outputData+1] = "            {\"Frame\" : " .. subChannel:GetKeyWhen(l) ..", \"Value\" : { "
												.. "\"r\" : ".. currentVal.r
												.. " ,\"g\" : ".. currentVal.g
												.. " ,\"b\" : ".. currentVal.b
												.. " ,\"a\" : ".. currentVal.a .." }}, "
											--print("Frame:" .. subChannel:GetKeyWhen(l) .. " value:" .. currentVal.r .. " , " .. currentVal.g .. " , " .. currentVal.b .. " , " .. currentVal.a )
										end
										outputData[#outputData+1] = "        ]"
										outputData[#outputData+1] = "    },"
									end
								end
							end
						end
					end
					
					if isColorChannel then
					--outputDataの表示
						for i=1,#outputData do
							--print(outputData[i])
							jsonData[#jsonData+1] = outputData[i]
						end
					end
				end
				--end --debug
				
				
			end
		
		end
	until not layer   
--	print("    ]")
--	print("}")
	
	jsonData[#jsonData+1] = "    ]"
	jsonData[#jsonData+1] = "}"
	
	--print("--------------json end------------------")
	print("Total number of layers in the project: " .. count)

--File Save
	--ファイル名の所得と拡張子の削除
	local fileName = moho.document:Name()
	fileName = string.reverse(fileName)
	placeNum = fileName:find("ohom.")
	fileName = string.reverse(fileName)
	fileName = string.sub(fileName,0,#fileName - (placeNum+4))
	--print(fileName)

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
	
	for i=1,#jsonData do
		file:write(jsonData[i] .. "\n")
	end
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
